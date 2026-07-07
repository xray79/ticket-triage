using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel;

namespace Shared.Infrastructure.Outbox;

public static class OutboxAppender
{
    /// <summary>
    /// Call from a module's DbContext.SaveChangesAsync override, before base.SaveChangesAsync,
    /// so the outbox row lands in the same transaction as the business change.
    /// </summary>
    public static void AppendEventsToOutbox(DbContext context, DbSet<OutboxMessage> outbox)
    {
        var aggregatesWithEvents = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregatesWithEvents)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                outbox.Add(new OutboxMessage
                {
                    Id = domainEvent.Id,
                    Type = domainEvent.GetType().AssemblyQualifiedName!,
                    Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                    OccurredOnUtc = domainEvent.OccurredOnUtc
                });
            }

            aggregate.ClearDomainEvents();
        }
    }
}
