using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure;

public sealed class IdentityDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public const string Schema = "identity";

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OrgSettingsEntity> OrgSettings => Set<OrgSettingsEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema);

        builder.Entity<RefreshToken>(rt =>
        {
            rt.ToTable("refresh_tokens");
            rt.HasKey(x => x.Id);
            rt.Property(x => x.TokenHash).IsRequired().HasMaxLength(200);
            rt.HasIndex(x => x.TokenHash).IsUnique();
        });

        builder.Entity<OrgSettingsEntity>(os =>
        {
            os.ToTable("org_settings");
            os.HasKey(x => x.Id);
        });
    }
}
