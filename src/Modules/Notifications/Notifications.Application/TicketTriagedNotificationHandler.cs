using Microsoft.Extensions.Logging;
using Notifications.Domain;
using Shared.Abstractions.Messaging;
using Triage.Contracts.Events;

namespace Notifications.Application;

public sealed class TicketTriagedNotificationHandler : IIntegrationEventHandler<TicketTriaged>
{
    private readonly INotificationLogRepository _repository;
    private readonly INotificationsUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<TicketTriagedNotificationHandler> _logger;

    public TicketTriagedNotificationHandler(
        INotificationLogRepository repository,
        INotificationsUnitOfWork unitOfWork,
        IEmailSender emailSender,
        ILogger<TicketTriagedNotificationHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task HandleAsync(TicketTriaged integrationEvent, CancellationToken ct)
    {
        if (await _repository.ExistsAsync(integrationEvent.TicketId, NotificationType.TicketTriaged, ct))
            return;

        if (string.IsNullOrWhiteSpace(integrationEvent.CustomerEmail))
        {
            _logger.LogWarning("No customer email on TicketTriaged for ticket {TicketId}; skipping notification.", integrationEvent.TicketId);
            return;
        }

        await _emailSender.SendAsync(
            integrationEvent.CustomerEmail,
            "We've reviewed your support ticket",
            $"Your ticket has been triaged as {integrationEvent.Category} ({integrationEvent.Priority} priority). " +
            "An agent will follow up shortly.",
            ct);

        _repository.Add(NotificationLog.Create(integrationEvent.TicketId, NotificationType.TicketTriaged, integrationEvent.CustomerEmail));
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
