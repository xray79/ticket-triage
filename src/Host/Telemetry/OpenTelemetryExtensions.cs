using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Triage.Application.Providers;

namespace Host.Telemetry;

/// <summary>
/// Traces the redaction pass and the LLM call as spans (via ASP.NET Core/HttpClient/EF Core
/// auto-instrumentation) and exports TriageMetrics' counters — end to end including the async
/// SQS hop, since the correlation id set by CorrelationIdMiddleware rides along in log scope
/// and this pipeline's traces share the same activity context. Exports to an OTLP collector
/// when configured (real environments); otherwise to the console, so tracing is visible
/// without standing up Tempo/Jaeger/X-Ray just to run locally.
/// </summary>
public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddTicketTriageTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var otlpEndpoint = configuration["Otel:OtlpEndpoint"];
        var serviceName = configuration["Otel:ServiceName"] ?? "TicketTriage.Host";

        var resourceBuilder = ResourceBuilder.CreateDefault().AddService(serviceName);

        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                    tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                else
                    tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(TriageMetrics.MeterName);

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                    metrics.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                else
                    metrics.AddConsoleExporter();
            });

        return services;
    }
}
