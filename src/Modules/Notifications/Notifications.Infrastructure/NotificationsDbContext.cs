using Microsoft.EntityFrameworkCore;
using Notifications.Domain;

namespace Notifications.Infrastructure;

public sealed class NotificationsDbContext : DbContext
{
    public const string Schema = "notifications";

    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options)
    {
    }

    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<NotificationLog>(nl =>
        {
            nl.ToTable("notification_logs");
            nl.HasKey(x => x.Id);
            nl.Property(x => x.RecipientEmail).IsRequired().HasMaxLength(320);
            nl.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
            nl.HasIndex(x => new { x.TicketId, x.Type }).IsUnique();
        });
    }
}
