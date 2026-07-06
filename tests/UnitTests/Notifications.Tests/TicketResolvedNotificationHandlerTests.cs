using FluentAssertions;
using NSubstitute;
using Notifications.Application;
using Notifications.Domain;
using Tickets.Contracts.Events;
using Xunit;

namespace Notifications.Tests;

public sealed class TicketResolvedNotificationHandlerTests
{
    [Fact]
    public async Task HandleAsync_sends_an_email_on_first_delivery()
    {
        var repository = Substitute.For<INotificationLogRepository>();
        repository.ExistsAsync(Arg.Any<Guid>(), NotificationType.TicketResolved, Arg.Any<CancellationToken>()).Returns(false);
        var unitOfWork = Substitute.For<INotificationsUnitOfWork>();
        var emailSender = Substitute.For<IEmailSender>();
        var handler = new TicketResolvedNotificationHandler(repository, unitOfWork, emailSender);

        var evt = new TicketResolved(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), "customer@example.com");
        await handler.HandleAsync(evt, CancellationToken.None);

        await emailSender.Received(1).SendAsync("customer@example.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        repository.Received(1).Add(Arg.Is<NotificationLog>(l => l.Type == NotificationType.TicketResolved));
    }

    [Fact]
    public async Task HandleAsync_is_idempotent_for_a_redelivered_message()
    {
        var repository = Substitute.For<INotificationLogRepository>();
        repository.ExistsAsync(Arg.Any<Guid>(), NotificationType.TicketResolved, Arg.Any<CancellationToken>()).Returns(true);
        var unitOfWork = Substitute.For<INotificationsUnitOfWork>();
        var emailSender = Substitute.For<IEmailSender>();
        var handler = new TicketResolvedNotificationHandler(repository, unitOfWork, emailSender);

        var evt = new TicketResolved(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), "customer@example.com");
        await handler.HandleAsync(evt, CancellationToken.None);

        await emailSender.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
