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
            var realRequestCount = 0; // Actual HTTP requests fired (after URL deduplication)
            var onlineCount = 0;
            var offlineCount = 0;
            var totalResponseTimeMs = 0L;

            using var scope = _scopefactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var checker = scope.ServiceProvider.GetRequiredService<UptimeCheckerService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var websitesToCheck = await context.Websites
                .Include(w => w.User)
                .Where(w => w.IsActive && w.NextCheckAt <= DateTime.UtcNow)
                .ToListAsync(stoppingToken);

            if (websitesToCheck.Any())
            {
                // Group websites by their normalized URL to avoid firing duplicate HTTP requests
                // for the same target (e.g. "Google.com" and "google.com" are treated as one).
                var groupedByUrl = websitesToCheck.GroupBy(w => NormalizeUrl(w.Url));

                foreach (var group in groupedByUrl)
                {
                    var websitesInGroup = group.ToList();
                    var representative = websitesInGroup[0];

                    UptimeCheck? sharedResult;

                    try
                    {
                        sharedResult = await checker.CheckWebsiteAsync(representative);
                        realRequestCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking shared URL {Url}", group.Key);

                        // Defer all websites in this group to the next cycle on failure.
                        foreach (var site in websitesInGroup)
                            site.NextCheckAt = DateTime.UtcNow.AddSeconds(site.CheckIntervalSeconds);

                        continue;
                    }

                    foreach (var website in websitesInGroup)
                    {
                        // Clone the shared result into a separate UptimeCheck entity for each
                        // website so that EF Core inserts N distinct rows (one per website).
                        var check = CloneCheckFor(sharedResult, website.Id);

                        var wasOnline = website.IsOnline;

                        if (wasOnline != false && !check.IsOnline)
                        {
                            context.DowntimeEvents.Add(new DowntimeEvent
                            {
                                WebsiteId = website.Id,
                                StartedAt = check.CheckedAt,
                                Reason = check.ErrorMessage ?? $"Status code: {check.StatusCode}"
                            });

                            _logger.LogWarning(
                                "[DOWN] {Name} ({Url}) went OFFLINE. StatusCode={StatusCode}, Error={Error}",
                                website.Name, website.Url, check.StatusCode, check.ErrorMessage
                            );

                            await notificationService.SendUptimeAlertAsync(
                                website, false, check.ErrorMessage ?? $"Status code: {check.StatusCode}");
                        }
                        else if (wasOnline == false && check.IsOnline)
                        {
                            var openDowntime = await context.DowntimeEvents
                                .FirstOrDefaultAsync(
                                    d => d.WebsiteId == website.Id && d.EndedAt == null,
                                    stoppingToken
                                );

                            if (openDowntime != null)
                            {
                                openDowntime.EndedAt = check.CheckedAt;
                            }

                            _logger.LogInformation(
                                "[UP] {Name} ({Url}) is back ONLINE after downtime.",
                                website.Name, website.Url
                            );

                            await notificationService.SendUptimeAlertAsync(
                                website, true, "All checks passed.");
                        }

                        website.IsOnline = check.IsOnline;
                        website.LastStatusCode = check.StatusCode;
                        website.LastResponseTimeMs = check.ResponseTimeMs;
                        website.LastCheckedAt = check.CheckedAt;
                        website.NextCheckAt = check.CheckedAt.AddSeconds(website.CheckIntervalSeconds);

                        context.UptimeChecks.Add(check);

                        checkedCount++;
                        if (check.IsOnline) onlineCount++;
                        else offlineCount++;
                        totalResponseTimeMs += check.ResponseTimeMs;

                        _logger.LogInformation(
                            "Checked {Url}: {Status} | StatusCode: {StatusCode} | ResponseTime: {ResponseTime}ms | NextCheck: {NextCheck}",
                            website.Url,
                            check.IsOnline ? "Online" : "Offline",
                            check.StatusCode,
                            check.ResponseTimeMs,
                            website.NextCheckAt.ToString("HH:mm:ss")
                        );
                    }
                }

                await context.SaveChangesAsync(stoppingToken);
            }

            if (DateTime.UtcNow.Date > lastCleanupDate.Date)
            {
                try
                {
                    var cutoffDate = DateTime.UtcNow.AddDays(-_options.RetentionDays);

                    int deletedRows = await context.UptimeChecks
                        .Where(c => c.CheckedAt < cutoffDate)
                        .ExecuteDeleteAsync(stoppingToken);

                    if (deletedRows > 0)
                        _logger.LogInformation("[Retention] Deleted {Count} old UptimeCheck records older than {CutoffDate}",
                            deletedRows, cutoffDate.ToString("yyyy-MM-dd"));

                    int deletedEvents = await context.DowntimeEvents
                        .Where(e => e.EndedAt != null && e.EndedAt < cutoffDate)
                        .ExecuteDeleteAsync(stoppingToken);

                    if (deletedEvents > 0)
                        _logger.LogInformation("[Retention] Deleted {Count} old DowntimeEvent records", deletedEvents);

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
                var failureRate = (double)offlineCount / checkedCount;

                _logger.LogInformation(
                    "[UptimeMetrics] Checked={CheckedCount}, RealRequests={RealRequests}, Online={OnlineCount}, Offline={OfflineCount}, AverageResponseTimeMs={AverageResponseTimeMs}, FailureRate={FailureRate:P2}, LoopElapsedMs={LoopElapsedMs}",
                    checkedCount, realRequestCount, onlineCount, offlineCount,
                    averageResponseTimeMs, failureRate, elapsedMs
                );
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.SchedulerDelaySeconds), stoppingToken);
        }
    }


    private static UptimeCheck CloneCheckFor(UptimeCheck source, int websiteId) => new()
    {
        WebsiteId = websiteId,
        IsOnline = source.IsOnline,
        StatusCode = source.StatusCode,
        ResponseTimeMs = source.ResponseTimeMs,
        ErrorMessage = source.ErrorMessage,
        CheckedAt = source.CheckedAt
    };

    private static string NormalizeUrl(string url)
    {
        if (Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri))
        {
            // Reconstruct with lowercased scheme+host; preserve everything else.
            var normalized = uri.GetLeftPart(UriPartial.Scheme).ToLowerInvariant()
                + uri.Host.ToLowerInvariant()
                + (uri.IsDefaultPort ? string.Empty : $":{uri.Port}")
                + (uri.AbsolutePath == "/" ? string.Empty : uri.AbsolutePath)
                + uri.Query
                + uri.Fragment;
            return normalized;
        }

        // Fallback: lowercase the whole string if it cannot be parsed as a URI.
        return (url ?? string.Empty).Trim().ToLowerInvariant();
    }
}