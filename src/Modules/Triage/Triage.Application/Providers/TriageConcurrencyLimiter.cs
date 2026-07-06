using Polly;
using Polly.Bulkhead;

namespace Triage.Application.Providers;

/// <summary>
/// One shared bulkhead across every provider — local and cloud alike — so a burst of
/// incoming tickets can't spawn unbounded concurrent LLM calls and starve the local
/// GPU or the API's own thread pool. Per-provider Polly policies (retry, circuit
/// breaker, timeout) stay in each Infrastructure HTTP client; this is the one
/// cross-cutting limit that has to apply to the whole triage pipeline, not one client.
/// </summary>
public interface ITriageConcurrencyLimiter
{
    Task<TriageResult> ExecuteAsync(Func<CancellationToken, Task<TriageResult>> action, CancellationToken ct);
}

public sealed class TriageConcurrencyLimiter : ITriageConcurrencyLimiter
{
    private readonly AsyncBulkheadPolicy<TriageResult> _policy;

    public TriageConcurrencyLimiter(TriageConcurrencyOptions options)
    {
        _policy = Policy.BulkheadAsync<TriageResult>(
            maxParallelization: options.MaxConcurrentTriages,
            maxQueuingActions: options.MaxQueuedTriages);
    }

    public Task<TriageResult> ExecuteAsync(Func<CancellationToken, Task<TriageResult>> action, CancellationToken ct) =>
        _policy.ExecuteAsync(action, ct);
}

public sealed class TriageConcurrencyOptions
{
    public const string SectionName = "Triage:Concurrency";

    /// <summary>Concurrent triage calls in flight at once, across every provider combined.</summary>
    public int MaxConcurrentTriages { get; set; } = 4;

    /// <summary>Additional callers allowed to queue once the limiter is full before being rejected.</summary>
    public int MaxQueuedTriages { get; set; } = 20;
}
