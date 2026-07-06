using Microsoft.EntityFrameworkCore;

namespace Shared.Infrastructure.Outbox;

/// <summary>Implemented by each module's DbContext so the generic dispatcher can reach its outbox table.</summary>
public interface IHasOutbox
{
    DbSet<OutboxMessage> OutboxMessages { get; }
}
