using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PulseWatch.Api.Models;

namespace PulseWatch.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Website> Websites => Set<Website>();
    public DbSet<UptimeCheck> UptimeChecks => Set<UptimeCheck>();
    public DbSet<DowntimeEvent> DowntimeEvents => Set<DowntimeEvent>();
    public DbSet<InAppNotification> InAppNotifications => Set<InAppNotification>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Website>(entity =>
        {
            entity.HasOne(w => w.User)
                  .WithMany()
                  .HasForeignKey(w => w.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(w => new { w.IsActive, w.NextCheckAt })
                  .HasDatabaseName("IX_Websites_IsActive_NextCheckAt");
        });

        builder.Entity<UptimeCheck>(entity =>
        {
            entity.HasIndex(c => new { c.WebsiteId, c.CheckedAt });
        });

        builder.Entity<InAppNotification>(entity =>
        {
            entity.HasOne(n => n.User)
                  .WithMany()
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.Website)
                  .WithMany()
                  .HasForeignKey(n => n.WebsiteId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(n => new { n.UserId, n.IsRead });
        });
    }
}