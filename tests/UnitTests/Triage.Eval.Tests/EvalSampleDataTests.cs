using FluentAssertions;
using Triage.Eval;

namespace Triage.Eval.Tests;

/// <summary>
/// A regression suite for the fixed sample set itself, not the scoring logic — catches a broken or
/// degenerate samples.json (dupes, blank fields, an out-of-vocabulary category) before it silently
/// invalidates every eval run.
/// </summary>
public sealed class EvalSampleDataTests
{
    private static readonly string[] ValidCategories = { "billing", "technical", "account", "general" };
    private static readonly string[] ValidPriorities = { "low", "medium", "high", "urgent" };

    private static IReadOnlyList<EvalSample> LoadSamples() =>
        EvalSampleLoader.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "samples.json"));

    [Fact]
    public void Sample_count_is_within_the_plans_30_to_50_range()
    {
        var samples = LoadSamples();

        samples.Count.Should().BeInRange(30, 50);
    }

    [Fact]
    public void Every_sample_has_a_unique_non_empty_id()
    {
        var samples = LoadSamples();

        samples.Should().OnlyContain(s => !string.IsNullOrWhiteSpace(s.Id));
        samples.Select(s => s.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Every_sample_has_non_empty_subject_and_body()
    {
        var samples = LoadSamples();

        samples.Should().OnlyContain(s => !string.IsNullOrWhiteSpace(s.Subject) && !string.IsNullOrWhiteSpace(s.Body));
    }

    [Fact]
    public void Every_sample_expects_one_of_the_four_valid_categories()
    {
        var samples = LoadSamples();

        samples.Should().OnlyContain(s => ValidCategories.Contains(s.ExpectedCategory));
    }

    [Fact]
    public void Every_sample_expects_one_of_the_four_valid_priorities()
    {
        var samples = LoadSamples();

        samples.Should().OnlyContain(s => ValidPriorities.Contains(s.ExpectedPriority));
    }

    [Fact]
    public void Every_sample_has_at_least_one_summary_keyword_to_score_against()
    {
        var samples = LoadSamples();

        samples.Should().OnlyContain(s => s.SummaryKeywords.Length > 0);
    }

    [Fact]
    public void Every_category_is_represented_by_at_least_five_samples()
    {
        var samples = LoadSamples();

        foreach (var category in ValidCategories)
            samples.Count(s => s.ExpectedCategory == category).Should().BeGreaterThanOrEqualTo(5);
    }
}
