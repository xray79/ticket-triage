using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Outbox;
using Tickets.Domain;

namespace Tickets.Infrastructure;

public sealed class TicketsDbContext : DbContext, IHasOutbox
{
    public const string Schema = "tickets";

    public TicketsDbContext(DbContextOptions<TicketsDbContext> options) : base(options)
    {
    }

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TicketsDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OutboxAppender.AppendEventsToOutbox(this, OutboxMessages);
        return base.SaveChangesAsync(cancellationToken);
    }
}
