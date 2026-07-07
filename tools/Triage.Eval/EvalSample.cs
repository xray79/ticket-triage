namespace Triage.Eval;

/// <summary>A fixed sample ticket with the category/priority/summary quality a triage call should produce.</summary>
public sealed record EvalSample(
    string Id,
    string Subject,
    string Body,
    string ExpectedCategory,
    string ExpectedPriority,
    string[] SummaryKeywords);
