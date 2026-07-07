using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging;
using Shared.Infrastructure.Outbox;
using Tickets.Contracts.Events;
using Triage.Application.Providers;
using Triage.Application.Redaction;
using Triage.Infrastructure.Providers;
using Triage.Infrastructure.Redaction;

namespace Triage.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTriageInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var options = new TriageOptions();
        configuration.GetSection(TriageOptions.SectionName).Bind(options);
        services.Configure<TriageOptions>(configuration.GetSection(TriageOptions.SectionName));

        services.AddDbContext<TriageDbContext>(dbOptions =>
            dbOptions.UseNpgsql(configuration.GetConnectionString("Triage"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", TriageDbContext.Schema)));

        services.AddScoped<Application.Abstractions.ITriageRecordRepository, TriageRecordRepository>();
        services.AddScoped<Application.Abstractions.ITriageUnitOfWork>(sp =>
            (Application.Abstractions.ITriageUnitOfWork)sp.GetRequiredService<Application.Abstractions.ITriageRecordRepository>());

        // Presidio: deterministic primary pass. Ollama: supplementary contextual pass — both feed the union.
        services.AddHttpClient<PresidioPiiDetector>(c => c.BaseAddress = new Uri(options.Presidio.BaseUrl));
        services.AddScoped<IPiiDetector, PresidioPiiDetector>();

        services.AddHttpClient<OllamaPiiDetectorHttpClient>(c =>
        {
            c.BaseAddress = new Uri(options.Ollama.BaseUrl);
            c.Timeout = TimeSpan.FromSeconds(options.Ollama.TimeoutSeconds);
        });
        services.AddScoped<IPiiDetector>(sp =>
            new OllamaPiiDetector(
                sp.GetRequiredService<OllamaPiiDetectorHttpClient>().Client,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OllamaPiiDetector>>(),
                options.Ollama.Model));

        // Local floor of the fallback chain — long timeout, no rate limiting, no circuit breaker.
        services.AddHttpClient<OllamaTriageClientHttpClient>(c =>
        {
            c.BaseAddress = new Uri(options.Ollama.BaseUrl);
            c.Timeout = TimeSpan.FromSeconds(options.Ollama.TimeoutSeconds);
        });
        services.AddKeyedScoped<ITriageLlmClient>(LlmProviderFactory.LocalKey, (sp, _) =>
            new OllamaTriageClient(sp.GetRequiredService<OllamaTriageClientHttpClient>().Client, options.Ollama.Model));

        // Cloud providers: rate-limit-aware retry + circuit breaker + short timeout.
        services.AddHttpClient<OpenAiTriageClientHttpClient>(c =>
            {
                c.BaseAddress = new Uri(options.OpenAi.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(options.OpenAi.TimeoutSeconds);
            })
            .AddResilienceHandler("openai", ConfigureCloudResilience);
        services.AddKeyedScoped<ITriageLlmClient>("openai", (sp, _) =>
            new OpenAiTriageClient(sp.GetRequiredService<OpenAiTriageClientHttpClient>().Client, options.OpenAi.ApiKey, options.OpenAi.Model));

        services.AddHttpClient<AnthropicTriageClientHttpClient>(c =>
            {
                c.BaseAddress = new Uri(options.Anthropic.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(options.Anthropic.TimeoutSeconds);
            })
            .AddResilienceHandler("anthropic", ConfigureCloudResilience);
        services.AddKeyedScoped<ITriageLlmClient>("anthropic", (sp, _) =>
            new AnthropicTriageClient(sp.GetRequiredService<AnthropicTriageClientHttpClient>().Client, options.Anthropic.ApiKey, options.Anthropic.Model));

        services.AddHttpClient<GeminiTriageClientHttpClient>(c =>
            {
                c.BaseAddress = new Uri(options.Gemini.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(options.Gemini.TimeoutSeconds);
            })
            .AddResilienceHandler("gemini", ConfigureCloudResilience);
        services.AddKeyedScoped<ITriageLlmClient>("gemini", (sp, _) =>
            new GeminiTriageClient(sp.GetRequiredService<GeminiTriageClientHttpClient>().Client, options.Gemini.ApiKey, options.Gemini.Model));

        services.AddScoped<ILlmProviderFactory, LlmProviderFactory>();

        services.AddHostedService<OutboxDispatcherHostedService<TriageDbContext>>();

        var triageInboxQueueUrl = configuration["Sqs:Queues:TriageInbox"];
        if (!string.IsNullOrWhiteSpace(triageInboxQueueUrl))
        {
            services.AddSqsConsumer(triageInboxQueueUrl, new[]
            {
                new IntegrationEventRoute(
                    nameof(TicketCreated),
                    typeof(TicketCreated),
                    (sp, evt, ct) => sp.GetRequiredService<IIntegrationEventHandler<TicketCreated>>()
                        .HandleAsync((TicketCreated)evt, ct)),
            });
        }

        return services;
    }

    /// <summary>Retry with backoff+jitter, circuit breaker, and timeout — respects Retry-After where the provider sends it.</summary>
    private static void ConfigureCloudResilience(ResiliencePipelineBuilder<HttpResponseMessage> builder)
    {
        builder
            .AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
            })
            .AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
            })
            .AddTimeout(TimeSpan.FromSeconds(15));
    }
}

// Thin typed-HttpClient wrappers so plain HttpClient can be shared between a keyed-service
// factory delegate and AddHttpClient's per-type client pool without name-based lookups.
public sealed class OllamaPiiDetectorHttpClient { public HttpClient Client { get; } public OllamaPiiDetectorHttpClient(HttpClient client) => Client = client; }
public sealed class OllamaTriageClientHttpClient { public HttpClient Client { get; } public OllamaTriageClientHttpClient(HttpClient client) => Client = client; }
public sealed class OpenAiTriageClientHttpClient { public HttpClient Client { get; } public OpenAiTriageClientHttpClient(HttpClient client) => Client = client; }
public sealed class AnthropicTriageClientHttpClient { public HttpClient Client { get; } public AnthropicTriageClientHttpClient(HttpClient client) => Client = client; }
public sealed class GeminiTriageClientHttpClient { public HttpClient Client { get; } public GeminiTriageClientHttpClient(HttpClient client) => Client = client; }
