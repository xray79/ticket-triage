using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triage.Domain;

namespace Triage.Infrastructure;

public sealed class TriageRecordConfiguration : IEntityTypeConfiguration<TriageRecord>
{
    public void Configure(EntityTypeBuilder<TriageRecord> builder)
    {
        builder.ToTable("triage_records");
        builder.HasKey(r => r.Id);

        // Enforces at the database level what TicketCreatedIntegrationEventHandler's
        // ExistsForTicketAsync check only approximates in application code: at most one
        // *succeeded* triage record per ticket, ever. Filtered (not a plain unique index) because
        // a failed attempt must not block a later successful retry for the same ticket — see
        // docs/concurrency/001-redelivered-ticket-created-race.md for the race this closes.
        builder.HasIndex(r => r.TicketId)
            .IsUnique()
            .HasFilter("\"Succeeded\" = true");

        builder.Property(r => r.Category).HasMaxLength(100);
        builder.Property(r => r.Priority).HasMaxLength(30);
        builder.Property(r => r.Summary).HasMaxLength(2000);
        builder.Property(r => r.DraftReply).HasMaxLength(4000);
        builder.Property(r => r.Provider).HasMaxLength(50);
        builder.Property(r => r.FailureReason).HasMaxLength(2000);

        builder.Ignore(r => r.DomainEvents);
    }
}
