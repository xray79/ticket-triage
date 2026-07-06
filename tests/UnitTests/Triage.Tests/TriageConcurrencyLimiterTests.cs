using FluentAssertions;
using Polly.Bulkhead;
using Triage.Application;
using Triage.Application.Providers;
using Xunit;

namespace Triage.Tests;

public sealed class TriageConcurrencyLimiterTests
{
    [Fact]
    public async Task ExecuteAsync_allows_calls_up_to_the_configured_limit_concurrently()
    {
        var limiter = new TriageConcurrencyLimiter(new TriageConcurrencyOptions { MaxConcurrentTriages = 2, MaxQueuedTriages = 0 });
        var gate = new TaskCompletionSource();

        var first = limiter.ExecuteAsync(async ct => { await gate.Task; return new TriageResult("a", "a", "a", "a"); }, CancellationToken.None);
        var second = limiter.ExecuteAsync(async ct => { await gate.Task; return new TriageResult("b", "b", "b", "b"); }, CancellationToken.None);

        // Both should be admitted (within the limit of 2); releasing the gate lets them complete.
        gate.SetResult();
        var results = await Task.WhenAll(first, second);

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_calls_beyond_the_limit_plus_queue()
    {
        var limiter = new TriageConcurrencyLimiter(new TriageConcurrencyOptions { MaxConcurrentTriages = 1, MaxQueuedTriages = 0 });
        var gate = new TaskCompletionSource();

        var blocking = limiter.ExecuteAsync(async ct => { await gate.Task; return new TriageResult("a", "a", "a", "a"); }, CancellationToken.None);

        // The slot is taken and there's no queue room, so a second call is rejected immediately
        // rather than piling up unboundedly behind a slow (or hung) provider call.
        var act = () => limiter.ExecuteAsync(_ => Task.FromResult(new TriageResult("b", "b", "b", "b")), CancellationToken.None);

        await act.Should().ThrowAsync<BulkheadRejectedException>();

        gate.SetResult();
        await blocking;
    }
}
