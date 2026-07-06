using Triage.Application;
using Triage.Application.Providers;

namespace Triage.Eval;

public static class EvalRunner
{
    /// <summary>
    /// Runs every sample through the given client sequentially (not concurrently) — this is a
    /// quality check run occasionally, not a load test, and sequential keeps output order stable
    /// and avoids tripping a provider's own rate limiting.
    /// </summary>
    public static async Task<IReadOnlyList<EvalResult>> RunAsync(
        IEnumerable<EvalSample> samples, ITriageLlmClient client, CancellationToken ct)
    {
        var results = new List<EvalResult>();
        foreach (var sample in samples)
        {
            var ticket = new TicketContent(Guid.NewGuid(), sample.Subject, sample.Body, "customer@example.com");
            var actual = await client.TriageAsync(ticket, ct);
            results.Add(EvalScorer.Score(sample, actual));
        }

        return results;
    }
}
