using Triage.Application;

namespace Triage.Application.Caching;

public sealed record CachedTriageAttempt(TriageResult Result, string Provider, bool WasFallback);

/// <summary>
/// Caches the triage outcome for a masked ticket's content so an identical or
/// near-identical ticket text coming in twice skips a redundant LLM call. Keyed on the
/// already-redacted text, never on raw PII. Backed by Redis in a real deployment, or an
/// in-memory store where no Redis is configured (see Host's DI wiring) — either way this
/// interface is what the rest of Triage depends on.
/// </summary>
public interface ITriageResultCache
{
    Task<CachedTriageAttempt?> GetAsync(string maskedSubject, string maskedBody, CancellationToken ct);
    Task SetAsync(string maskedSubject, string maskedBody, CachedTriageAttempt attempt, CancellationToken ct);
}
