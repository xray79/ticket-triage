using FluentAssertions;
using NSubstitute;
using Triage.Application;
using Triage.Application.Providers;
using Triage.Eval;

namespace Triage.Eval.Tests;

public sealed class EvalRunnerTests
{
    [Fact]
    public async Task RunAsync_scores_every_sample_against_the_client_response()
    {
        var samples = new[]
        {
            new EvalSample("s1", "subj1", "body1", "billing", "high", new[] { "refund" }),
            new EvalSample("s2", "subj2", "body2", "technical", "low", new[] { "bug" }),
        };

        var client = Substitute.For<ITriageLlmClient>();
        client.TriageAsync(Arg.Is<TicketContent>(t => t.Subject == "subj1"), Arg.Any<CancellationToken>())
            .Returns(new TriageResult("billing", "high", "processing your refund now", "draft"));
        client.TriageAsync(Arg.Is<TicketContent>(t => t.Subject == "subj2"), Arg.Any<CancellationToken>())
            .Returns(new TriageResult("general", "low", "not a bug, working as intended", "draft"));

        var results = await EvalRunner.RunAsync(samples, client, CancellationToken.None);

        results.Should().HaveCount(2);
        results[0].SampleId.Should().Be("s1");
        results[0].CategoryMatch.Should().BeTrue();
        results[0].PriorityMatch.Should().BeTrue();
        results[1].SampleId.Should().Be("s2");
        results[1].CategoryMatch.Should().BeFalse();
        results[1].PriorityMatch.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_passes_the_sample_text_through_unchanged_to_the_client()
    {
        var samples = new[] { new EvalSample("s1", "the subject", "the body", "general", "low", Array.Empty<string>()) };
        var client = Substitute.For<ITriageLlmClient>();
        client.TriageAsync(Arg.Any<TicketContent>(), Arg.Any<CancellationToken>())
            .Returns(new TriageResult("general", "low", "summary", "draft"));

        await EvalRunner.RunAsync(samples, client, CancellationToken.None);

        await client.Received(1).TriageAsync(
            Arg.Is<TicketContent>(t => t.Subject == "the subject" && t.Body == "the body"),
            Arg.Any<CancellationToken>());
    }
}
