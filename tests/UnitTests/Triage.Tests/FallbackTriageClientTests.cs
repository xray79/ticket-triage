using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Triage.Application;
using Triage.Application.Providers;
using Xunit;

namespace Triage.Tests;

public sealed class FallbackTriageClientTests
{
    private sealed class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options.Name);
        public void Dispose() { }
    }

    private static ITriageConcurrencyLimiter CreateLimiter() =>
        new TriageConcurrencyLimiter(new TriageConcurrencyOptions { MaxConcurrentTriages = 10, MaxQueuedTriages = 100 });

    private static TriageMetrics CreateMetrics() => new(new TestMeterFactory());

    private static ITriageLlmClient MakeClient(string key, TriageResult? result = null, Exception? throws = null)
    {
        var client = Substitute.For<ITriageLlmClient>();
        client.ProviderKey.Returns(key);
        if (throws is not null)
            client.TriageAsync(Arg.Any<TicketContent>(), Arg.Any<CancellationToken>()).ThrowsAsync(throws);
        else
            client.TriageAsync(Arg.Any<TicketContent>(), Arg.Any<CancellationToken>()).Returns(result!);
        return client;
    }

    [Fact]
    public async Task TriageAsync_uses_the_preferred_provider_when_it_succeeds()
    {
        var expected = new TriageResult("billing", "high", "summary", "draft");
        var openAi = MakeClient("openai", expected);
        var local = MakeClient("local", new TriageResult("x", "x", "x", "x"));

        var factory = Substitute.For<ILlmProviderFactory>();
        factory.Resolve("openai").Returns(openAi);
        factory.LocalClient.Returns(local);

        var orchestrator = new FallbackTriageClient(factory, CreateLimiter(), CreateMetrics(), NullLogger<FallbackTriageClient>.Instance);
        var ticket = new TicketContent(Guid.NewGuid(), "s", "b", "c@example.com");

        var attempt = await orchestrator.TriageAsync("openai", ticket, CancellationToken.None);

        attempt.Result.Should().Be(expected);
        attempt.Provider.Should().Be("openai");
        attempt.WasFallback.Should().BeFalse();
    }

    [Fact]
    public async Task TriageAsync_falls_back_to_local_when_the_preferred_provider_throws()
    {
        var localResult = new TriageResult("billing", "high", "summary", "draft");
        var openAi = MakeClient("openai", throws: new HttpRequestException("boom"));
        var local = MakeClient("local", localResult);

        var factory = Substitute.For<ILlmProviderFactory>();
        factory.Resolve("openai").Returns(openAi);
        factory.LocalClient.Returns(local);

        var orchestrator = new FallbackTriageClient(factory, CreateLimiter(), CreateMetrics(), NullLogger<FallbackTriageClient>.Instance);
        var ticket = new TicketContent(Guid.NewGuid(), "s", "b", "c@example.com");

        var attempt = await orchestrator.TriageAsync("openai", ticket, CancellationToken.None);

        attempt.Result.Should().Be(localResult);
        attempt.Provider.Should().Be("local");
        attempt.WasFallback.Should().BeTrue();
    }

    [Fact]
    public async Task TriageAsync_lets_a_local_failure_propagate_there_is_no_further_fallback()
    {
        var local = MakeClient("local", throws: new HttpRequestException("ollama down"));

        var factory = Substitute.For<ILlmProviderFactory>();
        factory.Resolve("local").Returns(local);
        factory.LocalClient.Returns(local);

        var orchestrator = new FallbackTriageClient(factory, CreateLimiter(), CreateMetrics(), NullLogger<FallbackTriageClient>.Instance);
        var ticket = new TicketContent(Guid.NewGuid(), "s", "b", "c@example.com");

        var act = () => orchestrator.TriageAsync("local", ticket, CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
