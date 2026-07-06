using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Triage.Application.Providers;

/// <summary>
/// Resolves the user's preferred provider, tries it through the shared concurrency limiter
/// (each client also has its own Polly retry/circuit-breaker/timeout policy at the HTTP layer
/// in Infrastructure), and on failure falls through to the local Ollama client — which has no
/// further fallback; it's the floor. Triage still completes for the ticket even when every
/// cloud attempt fails. Every attempt is recorded to <see cref="TriageMetrics"/>.
/// </summary>
public sealed class FallbackTriageClient : ITriageOrchestrator
{
    private readonly ILlmProviderFactory _factory;
    private readonly ITriageConcurrencyLimiter _limiter;
    private readonly TriageMetrics _metrics;
    private readonly ILogger<FallbackTriageClient> _logger;

    public FallbackTriageClient(
        ILlmProviderFactory factory,
        ITriageConcurrencyLimiter limiter,
        TriageMetrics metrics,
        ILogger<FallbackTriageClient> logger)
    {
        _factory = factory;
        _limiter = limiter;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<TriageAttempt> TriageAsync(string providerPreference, TicketContent maskedTicket, CancellationToken ct)
    {
        var preferred = _factory.Resolve(providerPreference);

        try
        {
            var result = await TimeAsync(preferred.ProviderKey, () => _limiter.ExecuteAsync(c => preferred.TriageAsync(maskedTicket, c), ct));
            return new TriageAttempt(result, preferred.ProviderKey, WasFallback: false);
        }
        catch (Exception ex) when (preferred.ProviderKey != _factory.LocalClient.ProviderKey)
        {
            _logger.LogWarning(ex,
                "Provider {Provider} failed for ticket {TicketId}; falling back to local.",
                preferred.ProviderKey, maskedTicket.TicketId);

            var localClient = _factory.LocalClient;
            var result = await TimeAsync(localClient.ProviderKey, () => _limiter.ExecuteAsync(c => localClient.TriageAsync(maskedTicket, c), ct), wasFallback: true);
            return new TriageAttempt(result, localClient.ProviderKey, WasFallback: true);
        }
    }

    private async Task<TriageResult> TimeAsync(string provider, Func<Task<TriageResult>> call, bool wasFallback = false)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await call();
            _metrics.RecordAttempt(provider, wasFallback, succeeded: true, stopwatch.Elapsed.TotalSeconds);
            return result;
        }
        catch
        {
            _metrics.RecordAttempt(provider, wasFallback, succeeded: false, stopwatch.Elapsed.TotalSeconds);
            throw;
        }
    }
}
