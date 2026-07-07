using Microsoft.EntityFrameworkCore;
using Notifications.Application;
using Notifications.Domain;

namespace Notifications.Infrastructure;

public sealed class NotificationLogRepository : INotificationLogRepository, INotificationsUnitOfWork
{
    private readonly NotificationsDbContext _context;

    public NotificationLogRepository(NotificationsDbContext context)
    {
        _context = context;
    }

    public void Add(NotificationLog log) => _context.NotificationLogs.Add(log);

    public Task<bool> ExistsAsync(Guid ticketId, NotificationType type, CancellationToken ct) =>
        _context.NotificationLogs.AnyAsync(x => x.TicketId == ticketId && x.Type == type, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
