using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PulseWatch.Api.Data;
using PulseWatch.Api.Models;

namespace PulseWatch.Api.Services;

public class UptimeBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopefactory;
    private readonly ILogger<UptimeBackgroundService> _logger;
    private readonly UptimeMonitoringOptions _options;

    public UptimeBackgroundService(
        IServiceScopeFactory scopefactory,
        ILogger<UptimeBackgroundService> logger,
        IOptions<UptimeMonitoringOptions> options)
    {
        _scopefactory = scopefactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "PulseWatch background service started. SchedulerDelaySeconds={SchedulerDelaySeconds}, RetentionDays={RetentionDays}",
            _options.SchedulerDelaySeconds,
            _options.RetentionDays
        );
        DateTime lastCleanupDate = DateTime.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            var loopStartedAt = DateTime.UtcNow;
            var checkedCount = 0;
            var onlineCount = 0;
            var offlineCount = 0;
            var totalResponseTimeMs = 0L;

            using var scope = _scopefactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var checker = scope.ServiceProvider.GetRequiredService<UptimeCheckerService>();

            var websites = await context.Websites
                                        .Where(w => w.IsActive)
                                        .ToListAsync(stoppingToken);

            if (websites.Any())
            {
                var websiteIds = websites.Select(w => w.Id).ToList();

                var openDowntimes = await context.DowntimeEvents
                    .Where(d => websiteIds.Contains(d.WebsiteId) && d.EndedAt == null)
                    .ToDictionaryAsync(d => d.WebsiteId, stoppingToken);

                var lastChecks = await context.UptimeChecks
                    .Where(c => websiteIds.Contains(c.WebsiteId))
                    .GroupBy(c => c.WebsiteId)
                    .Select(g => g.OrderByDescending(c => c.CheckedAt).FirstOrDefault())
                    .ToDictionaryAsync(c => c!.WebsiteId, stoppingToken);

                foreach (var website in websites)
                {
                    try
                    {
                        var check = await checker.CheckWebsiteAsync(website);

                        lastChecks.TryGetValue(website.Id, out var lastCheck);
                        openDowntimes.TryGetValue(website.Id, out var openDowntime);

                        if ((lastCheck == null || lastCheck.IsOnline) && !check.IsOnline)
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

                        checkedCount++;
                        if (check.IsOnline)
                        {
                            onlineCount++;
                        }
                        else
                        {
                            offlineCount++;
                        }

                        totalResponseTimeMs += check.ResponseTimeMs;

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

                await context.SaveChangesAsync(stoppingToken);
            }

            // Xử lý dọn dẹp dữ liệu cũ (Retention policy) duy trì một lần mỗi ngày
            if (DateTime.UtcNow.Date > lastCleanupDate.Date)
            {
                try
                {
                    var cutoffDate = DateTime.UtcNow.AddDays(-_options.RetentionDays);

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

                    int deletedEvents = await context.DowntimeEvents
                        .Where(e => e.EndedAt != null && e.EndedAt < cutoffDate)
                        .ExecuteDeleteAsync(stoppingToken);

                    if (deletedEvents > 0)
                    {
                        _logger.LogInformation(
                            "[Retention] Deleted {Count} old DowntimeEvent records",
                            deletedEvents
                        );
                    }
                  
                    lastCleanupDate = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running retention data cleanup policy.");
                }
            }

            if (checkedCount > 0)
            {
                var elapsedMs = (DateTime.UtcNow - loopStartedAt).TotalMilliseconds;
                var averageResponseTimeMs = totalResponseTimeMs / checkedCount;
                var failureRate = checkedCount == 0 ? 0 : (double)offlineCount / checkedCount;

                _logger.LogInformation(
                    "[UptimeMetrics] Checked={CheckedCount}, Online={OnlineCount}, Offline={OfflineCount}, AverageResponseTimeMs={AverageResponseTimeMs}, FailureRate={FailureRate:P2}, LoopElapsedMs={LoopElapsedMs}",
                    checkedCount,
                    onlineCount,
                    offlineCount,
                    averageResponseTimeMs,
                    failureRate,
                    elapsedMs
                );
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.SchedulerDelaySeconds), stoppingToken);
        }
    }
}