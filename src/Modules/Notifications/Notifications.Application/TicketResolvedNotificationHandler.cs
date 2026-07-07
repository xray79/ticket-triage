using Microsoft.Extensions.Logging;
using Notifications.Domain;
using Shared.Abstractions.Messaging;
using Tickets.Contracts.Events;

namespace Notifications.Application;

public sealed class TicketResolvedNotificationHandler : IIntegrationEventHandler<TicketResolved>
{
    private readonly INotificationLogRepository _repository;
    private readonly INotificationsUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;

    public TicketResolvedNotificationHandler(
        INotificationLogRepository repository,
        INotificationsUnitOfWork unitOfWork,
        IEmailSender emailSender)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _emailSender = emailSender;
    }

    public async Task HandleAsync(TicketResolved integrationEvent, CancellationToken ct)
    {
        if (await _repository.ExistsAsync(integrationEvent.TicketId, NotificationType.TicketResolved, ct))
            return;

        await _emailSender.SendAsync(
            integrationEvent.CustomerEmail,
            "Your support ticket has been resolved",
            "Your ticket has been marked resolved. Reply to this email if you need anything else.",
            ct);

        _repository.Add(NotificationLog.Create(integrationEvent.TicketId, NotificationType.TicketResolved, integrationEvent.CustomerEmail));
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
