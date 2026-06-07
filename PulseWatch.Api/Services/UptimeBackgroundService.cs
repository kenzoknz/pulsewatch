using Microsoft.EntityFrameworkCore;
using PulseWatch.Api.Data;
using PulseWatch.Api.Models;

namespace PulseWatch.Api.Services;

public class UptimeBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopefactory;
    private readonly ILogger<UptimeBackgroundService> _logger;

    public UptimeBackgroundService(IServiceScopeFactory scopefactory, ILogger<UptimeBackgroundService> logger)
    {
        _scopefactory = scopefactory;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("PulseWatch background service started.");
    DateTime lastCleanupDate = DateTime.MinValue;

    while (!stoppingToken.IsCancellationRequested)
    {
        using var scope = _scopefactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var checker = scope.ServiceProvider.GetRequiredService<UptimeCheckerService>();

        var websites = await context.Websites
                        .Where(w => w.IsActive)
                        .ToListAsync(stoppingToken);

        // VÒNG LẶP FOREACH: CHỈ LÀM NHIỆM VỤ PING VÀ ADD VÀO CONTEXT
        foreach (var website in websites)
        {
            try
            {
                var check = await checker.CheckWebsiteAsync(website);
                
                var lastCheck = await context.UptimeChecks
                                    .Where(c => c.WebsiteId == website.Id)
                                    .OrderByDescending(c => c.CheckedAt)
                                    .FirstOrDefaultAsync(stoppingToken);

                var openDowntime = await context.DowntimeEvents
                                    .Where(d => d.WebsiteId == website.Id && d.EndedAt == null)
                                    .FirstOrDefaultAsync(stoppingToken);

                if (lastCheck != null && lastCheck.IsOnline && !check.IsOnline)
                {
                    context.DowntimeEvents.Add(new DowntimeEvent
                    {
                        WebsiteId = website.Id,
                        StartedAt = check.CheckedAt,
                        Reason = check.ErrorMessage ?? $"Status code: {check.StatusCode}"
                    });
                }

                if (openDowntime != null && check.IsOnline)
                {
                    openDowntime.EndedAt = check.CheckedAt;
                }

                context.UptimeChecks.Add(check);
                
                _logger.LogInformation(
                    "Checked {Url}: {Status} | StatusCode: {StatusCode} | ResponseTime: {ResponseTime}ms",
                    website.Url, check.IsOnline ? "Online" : "Offline", check.StatusCode, check.ResponseTimeMs
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking website {WebsiteId}", website.Id);
            }
        } 
        {
            await context.SaveChangesAsync(stoppingToken);
        }

        if (DateTime.UtcNow.Date > lastCleanupDate.Date)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-90);
                int deletedRows = await context.UptimeChecks
                    .Where(c => c.CheckedAt < cutoffDate)
                    .ExecuteDeleteAsync(stoppingToken);

                if (deletedRows > 0)
                {
                    _logger.LogInformation(
                        "[Retention] Deleted {Count} old UptimeCheck records older than {CutoffDate}",
                        deletedRows, cutoffDate.ToString("yyyy-MM-dd")
                    );
                }

                lastCleanupDate = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running retention data cleanup policy.");
            }
        }

        // TỐI ƯU 3: Quét xong toàn bộ danh sách rồi mới nghỉ ngơi 5 phút
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
    }
}
}