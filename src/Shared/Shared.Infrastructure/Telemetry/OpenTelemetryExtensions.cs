using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shared.Infrastructure.Telemetry;

/// <summary>
/// Traces ASP.NET Core/HttpClient/EF Core spans and exports any given meters (e.g. a module's own
/// counters/histograms) — shared by every deployable in the solution (the main Host and any
/// extracted service) so each gets identical tracing/metrics wiring, distinguished only by its own
/// service name and whichever extra meters it owns. Exports to an OTLP collector when configured
/// (real environments); otherwise to the console, so tracing is visible without standing up
/// Tempo/Jaeger/X-Ray just to run locally.
/// </summary>
public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddTicketTriageTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        params string[] additionalMeterNames)
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
                    .AddRuntimeInstrumentation();

                foreach (var meterName in additionalMeterNames)
                    metrics.AddMeter(meterName);

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                    metrics.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                else
                    metrics.AddConsoleExporter();
            });

        return services;
    }
}
