namespace Triage.Application.Providers;

/// <summary>Resolves the keyed <see cref="ITriageLlmClient"/> registration for a user's provider
/// preference. Implemented in Infrastructure since it wraps .NET keyed-service resolution.</summary>
public interface ILlmProviderFactory
{
    ITriageLlmClient Resolve(string providerKey);

    /// <summary>The local Ollama client — the floor of the fallback chain, with no further fallback.</summary>
    ITriageLlmClient LocalClient { get; }
}
