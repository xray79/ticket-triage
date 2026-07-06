using FluentAssertions;
using NSubstitute;
using Reporting.Application;
using Reporting.Domain;
using Tickets.Contracts.Events;
using Triage.Contracts.Events;
using Xunit;

namespace Reporting.Tests;

public sealed class ReportingEventHandlersTests
{
    [Fact]
    public async Task TicketCreatedReportHandler_creates_a_row_on_first_delivery()
    {
        var repository = Substitute.For<ITicketReportRepository>();
        repository.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((TicketReportEntry?)null);
        var unitOfWork = Substitute.For<IReportingUnitOfWork>();
        var handler = new TicketCreatedReportHandler(repository, unitOfWork);

        var evt = new TicketCreated(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), "s", "b", "c@example.com", "local");
        await handler.HandleAsync(evt, CancellationToken.None);

        repository.Received(1).Add(Arg.Any<TicketReportEntry>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TicketCreatedReportHandler_is_idempotent_for_a_redelivered_message()
    {
        var repository = Substitute.For<ITicketReportRepository>();
        repository.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(TicketReportEntry.Create(Guid.NewGuid(), DateTimeOffset.UtcNow));
        var unitOfWork = Substitute.For<IReportingUnitOfWork>();
        var handler = new TicketCreatedReportHandler(repository, unitOfWork);

        var evt = new TicketCreated(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), "s", "b", "c@example.com", "local");
        await handler.HandleAsync(evt, CancellationToken.None);

        repository.DidNotReceive().Add(Arg.Any<TicketReportEntry>());
    }

    [Fact]
    public async Task TicketTriagedReportHandler_updates_an_existing_New_row()
    {
        var entry = TicketReportEntry.Create(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var repository = Substitute.For<ITicketReportRepository>();
        repository.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(entry);
        var unitOfWork = Substitute.For<IReportingUnitOfWork>();
        var handler = new TicketTriagedReportHandler(repository, unitOfWork);

        var evt = new TicketTriaged(Guid.NewGuid(), DateTimeOffset.UtcNow, entry.TicketId, "billing", "high", "s", "d", "local", false, "c@example.com");
        await handler.HandleAsync(evt, CancellationToken.None);

        entry.Status.Should().Be("Triaged");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TicketTriagedReportHandler_is_a_no_op_when_no_report_row_exists_yet()
    {
        var repository = Substitute.For<ITicketReportRepository>();
        repository.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((TicketReportEntry?)null);
        var unitOfWork = Substitute.For<IReportingUnitOfWork>();
        var handler = new TicketTriagedReportHandler(repository, unitOfWork);

        var evt = new TicketTriaged(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), "billing", "high", "s", "d", "local", false, "c@example.com");
        await handler.HandleAsync(evt, CancellationToken.None);

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TicketResolvedReportHandler_updates_an_existing_row()
    {
        var entry = TicketReportEntry.Create(Guid.NewGuid(), DateTimeOffset.UtcNow);
        entry.ApplyTriaged("billing", "high", "local", false, DateTimeOffset.UtcNow);
        var repository = Substitute.For<ITicketReportRepository>();
        repository.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(entry);
        var unitOfWork = Substitute.For<IReportingUnitOfWork>();
        var handler = new TicketResolvedReportHandler(repository, unitOfWork);

        var evt = new TicketResolved(Guid.NewGuid(), DateTimeOffset.UtcNow, entry.TicketId, "c@example.com");
        await handler.HandleAsync(evt, CancellationToken.None);

        entry.Status.Should().Be("Resolved");
    }
}
