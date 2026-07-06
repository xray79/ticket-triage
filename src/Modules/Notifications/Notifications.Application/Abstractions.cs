using Notifications.Domain;

namespace Notifications.Application;

public interface INotificationLogRepository
{
    void Add(NotificationLog log);
    Task<bool> ExistsAsync(Guid ticketId, NotificationType type, CancellationToken ct);
}

public interface INotificationsUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}

/// <summary>Sends the actual notification. Implemented by an SMTP client in a real
/// deployment, or a logging-only sender when no SMTP host is configured (see Host's DI
/// wiring) — same fallback pattern as the Redis cache and SQS ServiceUrl.</summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken ct);
}
