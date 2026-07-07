using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Outbox;
using Triage.Domain;

namespace Triage.Infrastructure;

public sealed class TriageDbContext : DbContext, IHasOutbox
{
    public const string Schema = "triage";

    public TriageDbContext(DbContextOptions<TriageDbContext> options) : base(options)
    {
    }

    public DbSet<TriageRecord> TriageRecords => Set<TriageRecord>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TriageDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OutboxAppender.AppendEventsToOutbox(this, OutboxMessages);
        return base.SaveChangesAsync(cancellationToken);
    }
}
