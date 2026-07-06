using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;
using Tickets.Contracts.Events;
using Triage.Application.Providers;
using Triage.Application.Redaction;

namespace Triage.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTriageApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IRedactionEngine, RedactionEngine>();
        services.AddScoped<ITriageOrchestrator, FallbackTriageClient>();
        services.AddScoped<IIntegrationEventHandler<TicketCreated>, TicketCreatedIntegrationEventHandler>();

        var concurrencyOptions = new TriageConcurrencyOptions();
        configuration.GetSection(TriageConcurrencyOptions.SectionName).Bind(concurrencyOptions);
        services.AddSingleton(concurrencyOptions);
        services.AddSingleton<ITriageConcurrencyLimiter, TriageConcurrencyLimiter>();

        services.AddMetrics();
        services.AddSingleton<TriageMetrics>();

        return services;
    }
}
