using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tickets.Contracts.Events;
using Triage.Application;
using Triage.Application.Abstractions;
using Triage.Application.Caching;
using Triage.Application.Providers;
using Triage.Application.Redaction;
using Triage.Domain;
using Xunit;

namespace Triage.Tests;

public sealed class TicketCreatedIntegrationEventHandlerTests
{
    private static TicketCreated MakeEvent(Guid? ticketId = null) => new(
        Guid.NewGuid(), DateTimeOffset.UtcNow, ticketId ?? Guid.NewGuid(),
        "Subject", "Body text", "customer@example.com", "local");

    private static RedactedTicket NoOpRedaction(string subject, string body) => new(
        new RedactedField(subject, new Dictionary<string, string>()),
        new RedactedField(body, new Dictionary<string, string>()));

    [Fact]
    public async Task HandleAsync_calls_the_orchestrator_on_a_cache_miss_and_populates_the_cache()
    {
        var repository = Substitute.For<ITriageRecordRepository>();
        var unitOfWork = Substitute.For<ITriageUnitOfWork>();
        var redaction = Substitute.For<IRedactionEngine>();
        redaction.RedactAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => NoOpRedaction((string)ci[0], (string)ci[1]));
        var orchestrator = Substitute.For<ITriageOrchestrator>();
        var result = new TriageResult("billing", "high", "summary", "draft");
        orchestrator.TriageAsync(Arg.Any<string>(), Arg.Any<TicketContent>(), Arg.Any<CancellationToken>())
            .Returns(new TriageAttempt(result, "local", false));
        var cache = Substitute.For<ITriageResultCache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((CachedTriageAttempt?)null);

        var handler = new TicketCreatedIntegrationEventHandler(redaction, orchestrator, cache, repository, unitOfWork, NullLogger<TicketCreatedIntegrationEventHandler>.Instance);

        await handler.HandleAsync(MakeEvent(), CancellationToken.None);

        await orchestrator.Received(1).TriageAsync(Arg.Any<string>(), Arg.Any<TicketContent>(), Arg.Any<CancellationToken>());
        await cache.Received(1).SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CachedTriageAttempt>(), Arg.Any<CancellationToken>());
        repository.Received(1).Add(Arg.Is<TriageRecord>(r => r.Succeeded));
    }

    [Fact]
    public async Task HandleAsync_skips_the_orchestrator_entirely_on_a_cache_hit()
    {
        var repository = Substitute.For<ITriageRecordRepository>();
        var unitOfWork = Substitute.For<ITriageUnitOfWork>();
        var redaction = Substitute.For<IRedactionEngine>();
        redaction.RedactAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => NoOpRedaction((string)ci[0], (string)ci[1]));
        var orchestrator = Substitute.For<ITriageOrchestrator>();
        var cachedResult = new TriageResult("technical", "low", "cached summary", "cached draft");
        var cache = Substitute.For<ITriageResultCache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CachedTriageAttempt(cachedResult, "openai", false));

        var handler = new TicketCreatedIntegrationEventHandler(redaction, orchestrator, cache, repository, unitOfWork, NullLogger<TicketCreatedIntegrationEventHandler>.Instance);

        await handler.HandleAsync(MakeEvent(), CancellationToken.None);

        await orchestrator.DidNotReceive().TriageAsync(Arg.Any<string>(), Arg.Any<TicketContent>(), Arg.Any<CancellationToken>());
        repository.Received(1).Add(Arg.Is<TriageRecord>(r => r.Succeeded && r.Category == "technical" && r.Provider == "openai"));
    }
}
