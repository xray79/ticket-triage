using Microsoft.Extensions.Logging;

namespace Triage.Application.Providers;

/// <summary>
/// Resolves the user's preferred provider, tries it (each client already has its own Polly
/// retry/circuit-breaker/timeout policy applied at the HTTP layer in Infrastructure), and on
/// failure falls through to the local Ollama client — which has no further fallback; it's the
/// floor. Triage still completes for the ticket even when every cloud attempt fails.
/// </summary>
public sealed class FallbackTriageClient : ITriageOrchestrator
{
    private readonly ILlmProviderFactory _factory;
    private readonly ILogger<FallbackTriageClient> _logger;

    public FallbackTriageClient(ILlmProviderFactory factory, ILogger<FallbackTriageClient> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<TriageAttempt> TriageAsync(string providerPreference, TicketContent maskedTicket, CancellationToken ct)
    {
        var preferred = _factory.Resolve(providerPreference);

        try
        {
            var result = await preferred.TriageAsync(maskedTicket, ct);
            return new TriageAttempt(result, preferred.ProviderKey, WasFallback: false);
        }
        catch (Exception ex) when (preferred.ProviderKey != _factory.LocalClient.ProviderKey)
        {
            _logger.LogWarning(ex,
                "Provider {Provider} failed for ticket {TicketId}; falling back to local.",
                preferred.ProviderKey, maskedTicket.TicketId);

            var localClient = _factory.LocalClient;
            var result = await localClient.TriageAsync(maskedTicket, ct);
            return new TriageAttempt(result, localClient.ProviderKey, WasFallback: true);
        }
    }
}
