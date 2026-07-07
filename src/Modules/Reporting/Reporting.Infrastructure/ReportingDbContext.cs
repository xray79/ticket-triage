using Microsoft.EntityFrameworkCore;
using Reporting.Domain;

namespace Reporting.Infrastructure;

public sealed class ReportingDbContext : DbContext
{
    public const string Schema = "reporting";

    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options)
    {
    }

    public DbSet<TicketReportEntry> TicketReportEntries => Set<TicketReportEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<TicketReportEntry>(e =>
        {
            e.ToTable("ticket_report_entries");
            e.HasKey(x => x.TicketId);
            e.Property(x => x.Status).HasMaxLength(30);
            e.Property(x => x.Category).HasMaxLength(100);
            e.Property(x => x.Priority).HasMaxLength(30);
            e.Property(x => x.Provider).HasMaxLength(50);
        });
    }
}
