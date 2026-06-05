using Microsoft.EntityFrameworkCore;
using PulseWatch.Api.Models;

namespace PulseWatch.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Website> Websites => Set<Website>();
    public DbSet<UptimeCheck> UptimeChecks => Set<UptimeCheck>();
    public DbSet<DowntimeEvent> DowntimeEvents => Set<DowntimeEvent>();
}