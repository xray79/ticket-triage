using FluentAssertions;
using Triage.Application;
using Triage.Eval;

namespace Triage.Eval.Tests;

public sealed class EvalScorerTests
{
    private static EvalSample MakeSample(string[]? keywords = null) => new(
        "sample-1", "subject", "body", "billing", "high", keywords ?? new[] { "refund", "duplicate" });

    [Fact]
    public void Score_matches_category_and_priority_case_insensitively()
    {
        var sample = MakeSample();
        var actual = new TriageResult("Billing", "HIGH", "please refund the duplicate charge", "draft");

        var result = EvalScorer.Score(sample, actual);

        result.CategoryMatch.Should().BeTrue();
        result.PriorityMatch.Should().BeTrue();
    }

    [Fact]
    public void Score_flags_a_category_mismatch()
    {
        var sample = MakeSample();
        var actual = new TriageResult("technical", "high", "summary", "draft");

        var result = EvalScorer.Score(sample, actual);

        result.CategoryMatch.Should().BeFalse();
        result.PriorityMatch.Should().BeTrue();
    }

    [Fact]
    public void Score_computes_partial_keyword_hit_rate()
    {
        var sample = MakeSample(new[] { "refund", "duplicate", "charge" });
        var actual = new TriageResult("billing", "high", "we will issue a refund shortly", "draft");

        var result = EvalScorer.Score(sample, actual);

        result.SummaryKeywordHitRate.Should().Be(1.0 / 3.0);
    }

    [Fact]
    public void Score_gives_full_keyword_credit_when_the_sample_has_no_keywords()
    {
        var sample = MakeSample(Array.Empty<string>());
        var actual = new TriageResult("billing", "high", "anything at all", "draft");

        var result = EvalScorer.Score(sample, actual);

        result.SummaryKeywordHitRate.Should().Be(1.0);
    }

    [Fact]
    public void Aggregate_computes_accuracy_across_all_results()
    {
        var results = new[]
        {
            new EvalResult("a", true, true, 1.0, new TriageResult("billing", "high", "s", "d")),
            new EvalResult("b", true, false, 0.5, new TriageResult("billing", "low", "s", "d")),
            new EvalResult("c", false, false, 0.0, new TriageResult("technical", "low", "s", "d")),
        };

        var summary = EvalScorer.Aggregate(results);

        summary.SampleCount.Should().Be(3);
        summary.CategoryAccuracy.Should().BeApproximately(2.0 / 3.0, 0.0001);
        summary.PriorityAccuracy.Should().BeApproximately(1.0 / 3.0, 0.0001);
        summary.AverageSummaryKeywordHitRate.Should().BeApproximately(0.5, 0.0001);
    }

    [Fact]
    public void Aggregate_of_no_results_is_all_zero_not_a_divide_by_zero_crash()
    {
        var summary = EvalScorer.Aggregate(Array.Empty<EvalResult>());

        summary.SampleCount.Should().Be(0);
        summary.CategoryAccuracy.Should().Be(0);
        summary.PriorityAccuracy.Should().Be(0);
        summary.AverageSummaryKeywordHitRate.Should().Be(0);
    }
}
