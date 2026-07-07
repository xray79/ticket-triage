namespace Triage.Application.Providers;

/// <summary>Implemented once per provider (Ollama, OpenAI, Anthropic, Gemini). Registered
/// as a keyed service under the provider's preference key ("local", "openai", "anthropic", "gemini").</summary>
public interface ITriageLlmClient
{
    /// <summary>The preference key this client is registered under, e.g. "local".</summary>
    string ProviderKey { get; }

    Task<TriageResult> TriageAsync(TicketContent maskedTicket, CancellationToken ct);
}
