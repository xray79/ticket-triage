using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Notifications.Application;
using Notifications.Domain;
using Triage.Contracts.Events;
using Xunit;

namespace Notifications.Tests;

public sealed class TicketTriagedNotificationHandlerTests
{
    private static TicketTriaged MakeEvent(Guid? ticketId = null, string customerEmail = "customer@example.com") => new(
        Guid.NewGuid(), DateTimeOffset.UtcNow, ticketId ?? Guid.NewGuid(),
        "billing", "high", "summary", "draft", "local", false, customerEmail);

    [Fact]
    public async Task HandleAsync_sends_an_email_and_logs_it_on_first_delivery()
    {
        var repository = Substitute.For<INotificationLogRepository>();
        repository.ExistsAsync(Arg.Any<Guid>(), NotificationType.TicketTriaged, Arg.Any<CancellationToken>()).Returns(false);
        var unitOfWork = Substitute.For<INotificationsUnitOfWork>();
        var emailSender = Substitute.For<IEmailSender>();
        var handler = new TicketTriagedNotificationHandler(repository, unitOfWork, emailSender, NullLogger<TicketTriagedNotificationHandler>.Instance);

        await handler.HandleAsync(MakeEvent(), CancellationToken.None);

        await emailSender.Received(1).SendAsync("customer@example.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        repository.Received(1).Add(Arg.Is<NotificationLog>(l => l.Type == NotificationType.TicketTriaged));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_is_idempotent_for_a_redelivered_message()
    {
        var repository = Substitute.For<INotificationLogRepository>();
        repository.ExistsAsync(Arg.Any<Guid>(), NotificationType.TicketTriaged, Arg.Any<CancellationToken>()).Returns(true);
        var unitOfWork = Substitute.For<INotificationsUnitOfWork>();
        var emailSender = Substitute.For<IEmailSender>();
        var handler = new TicketTriagedNotificationHandler(repository, unitOfWork, emailSender, NullLogger<TicketTriagedNotificationHandler>.Instance);

        await handler.HandleAsync(MakeEvent(), CancellationToken.None);

        await emailSender.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_skips_sending_when_customer_email_is_missing()
    {
        var repository = Substitute.For<INotificationLogRepository>();
        repository.ExistsAsync(Arg.Any<Guid>(), NotificationType.TicketTriaged, Arg.Any<CancellationToken>()).Returns(false);
        var unitOfWork = Substitute.For<INotificationsUnitOfWork>();
        var emailSender = Substitute.For<IEmailSender>();
        var handler = new TicketTriagedNotificationHandler(repository, unitOfWork, emailSender, NullLogger<TicketTriagedNotificationHandler>.Instance);

        await handler.HandleAsync(MakeEvent(customerEmail: ""), CancellationToken.None);

        await emailSender.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
