using FluentAssertions;
using NSubstitute;
using Tickets.Application.Abstractions;
using Tickets.Application.Tickets.CreateTicket;
using Tickets.Domain;
using Xunit;

namespace Tickets.Tests;

public sealed class CreateTicketCommandHandlerTests
{
    [Fact]
    public async Task Handle_persists_ticket_and_returns_its_id()
    {
        var repository = Substitute.For<ITicketRepository>();
        var unitOfWork = Substitute.For<ITicketsUnitOfWork>();
        var handler = new CreateTicketCommandHandler(repository, unitOfWork);

        var command = new CreateTicketCommand("Subject", "Body", "customer@example.com", "local", Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repository.Received(1).Add(Arg.Is<Ticket>(t => t.Id == result.Value));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_returns_failure_and_does_not_persist_when_domain_validation_fails()
    {
        var repository = Substitute.For<ITicketRepository>();
        var unitOfWork = Substitute.For<ITicketsUnitOfWork>();
        var handler = new CreateTicketCommandHandler(repository, unitOfWork);

        var command = new CreateTicketCommand("", "Body", "customer@example.com", "local", Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        repository.DidNotReceive().Add(Arg.Any<Ticket>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
