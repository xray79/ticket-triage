using Triage.Application;

namespace Triage.Eval;

public sealed record EvalResult(
    string SampleId,
    bool CategoryMatch,
    bool PriorityMatch,
    double SummaryKeywordHitRate,
    TriageResult Actual);

public sealed record EvalSummary(
    int SampleCount,
    double CategoryAccuracy,
    double PriorityAccuracy,
    double AverageSummaryKeywordHitRate,
    IReadOnlyList<EvalResult> Results);

/// <summary>
/// Deliberately simple, dependency-free scoring: exact match on category/priority (the model is
/// asked for one of a fixed set of values, so fuzzy matching would hide real misclassification),
/// and a keyword-overlap heuristic for summary quality — cheap enough to run in CI without a
/// second "LLM as judge" call, at the cost of not catching a fluent but factually wrong summary.
/// </summary>
public static class EvalScorer
{
    public static EvalResult Score(EvalSample sample, TriageResult actual) => new(
        sample.Id,
        string.Equals(sample.ExpectedCategory, actual.Category, StringComparison.OrdinalIgnoreCase),
        string.Equals(sample.ExpectedPriority, actual.Priority, StringComparison.OrdinalIgnoreCase),
        KeywordHitRate(sample.SummaryKeywords, actual.Summary),
        actual);

    public static EvalSummary Aggregate(IReadOnlyList<EvalResult> results)
    {
        if (results.Count == 0)
            return new EvalSummary(0, 0, 0, 0, results);

        return new EvalSummary(
            results.Count,
            results.Count(r => r.CategoryMatch) / (double)results.Count,
            results.Count(r => r.PriorityMatch) / (double)results.Count,
            results.Average(r => r.SummaryKeywordHitRate),
            results);
    }

    private static double KeywordHitRate(string[] keywords, string summary)
    {
        if (keywords.Length == 0)
            return 1.0;

        var hits = keywords.Count(k => summary.Contains(k, StringComparison.OrdinalIgnoreCase));
        return hits / (double)keywords.Length;
    }
}
