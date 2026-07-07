using FluentAssertions;
using NSubstitute;
using Tickets.Application.Abstractions;
using Tickets.Application.Tickets.ApplyTriageResult;
using Tickets.Domain;
using Triage.Contracts.Events;
using Xunit;

namespace Tickets.Tests;

public sealed class TicketTriagedIntegrationEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_applies_triage_result_and_saves()
    {
        var ticket = Ticket.Create("Subject", "Body", "a@b.com", "local", Guid.NewGuid()).Value;
        var repository = Substitute.For<ITicketRepository>();
        repository.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);
        var unitOfWork = Substitute.For<ITicketsUnitOfWork>();
        var handler = new TicketTriagedIntegrationEventHandler(repository, unitOfWork);

        var occurredOnUtc = DateTimeOffset.UtcNow;
        var evt = new TicketTriaged(Guid.NewGuid(), occurredOnUtc, ticket.Id, "billing", "high", "summary", "draft", "local", false);

        await handler.HandleAsync(evt, CancellationToken.None);

        ticket.Status.Should().Be(TicketStatus.Triaged);
        ticket.Triage!.Category.Should().Be("billing");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_is_idempotent_for_a_redelivered_message()
    {
        var ticket = Ticket.Create("Subject", "Body", "a@b.com", "local", Guid.NewGuid()).Value;
        var repository = Substitute.For<ITicketRepository>();
        repository.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);
        var unitOfWork = Substitute.For<ITicketsUnitOfWork>();
        var handler = new TicketTriagedIntegrationEventHandler(repository, unitOfWork);

        var occurredOnUtc = DateTimeOffset.UtcNow;
        var evt = new TicketTriaged(Guid.NewGuid(), occurredOnUtc, ticket.Id, "billing", "high", "summary", "draft", "local", false);

        await handler.HandleAsync(evt, CancellationToken.None);
        await handler.HandleAsync(evt, CancellationToken.None); // redelivered

        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_is_a_no_op_when_ticket_no_longer_exists()
    {
        var repository = Substitute.For<ITicketRepository>();
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Ticket?)null);
        var unitOfWork = Substitute.For<ITicketsUnitOfWork>();
        var handler = new TicketTriagedIntegrationEventHandler(repository, unitOfWork);

        var evt = new TicketTriaged(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), "billing", "high", "summary", "draft", "local", false);

        await handler.HandleAsync(evt, CancellationToken.None);

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
