using Shared.Kernel;

namespace Notifications.Domain;

/// <summary>
/// A record that a notification was sent for a given ticket + event type. Existence of a
/// row is the idempotency check — a redelivered SQS message for the same ticket/type is a
/// safe no-op rather than a duplicate email.
/// </summary>
public sealed class NotificationLog : Entity<Guid>
{
    private NotificationLog() { }

    public Guid TicketId { get; private set; }
    public NotificationType Type { get; private set; }
    public string RecipientEmail { get; private set; } = string.Empty;
    public DateTimeOffset SentAtUtc { get; private set; }

    public static NotificationLog Create(Guid ticketId, NotificationType type, string recipientEmail) => new()
    {
        Id = Guid.NewGuid(),
        TicketId = ticketId,
        Type = type,
        RecipientEmail = recipientEmail,
        SentAtUtc = DateTimeOffset.UtcNow
    };
}
