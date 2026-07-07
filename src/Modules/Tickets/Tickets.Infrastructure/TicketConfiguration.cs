using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tickets.Domain;

namespace Tickets.Infrastructure;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Subject).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Body).IsRequired().HasMaxLength(10_000);
        builder.Property(t => t.CustomerEmail).IsRequired().HasMaxLength(320);
        builder.Property(t => t.RequestedProvider).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(30);

        builder.OwnsOne(t => t.Triage, triage =>
        {
            triage.Property(x => x.Category).HasColumnName("triage_category").HasMaxLength(100);
            triage.Property(x => x.Priority).HasColumnName("triage_priority").HasMaxLength(30);
            triage.Property(x => x.Summary).HasColumnName("triage_summary").HasMaxLength(2000);
            triage.Property(x => x.DraftReply).HasColumnName("triage_draft_reply").HasMaxLength(4000);
            triage.Property(x => x.Provider).HasColumnName("triage_provider").HasMaxLength(50);
            triage.Property(x => x.WasFallback).HasColumnName("triage_was_fallback");
            triage.Property(x => x.TriagedAtUtc).HasColumnName("triage_triaged_at_utc");
        });

        builder.Navigation(t => t.Triage).IsRequired(false);

        builder.Ignore(t => t.DomainEvents);
    }
}
