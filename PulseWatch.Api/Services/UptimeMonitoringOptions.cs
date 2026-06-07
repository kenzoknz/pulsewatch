namespace PulseWatch.Api.Services;

public class UptimeMonitoringOptions
{
    public const string SectionName = "UptimeMonitoring";

    public int SchedulerDelaySeconds { get; set; } = 60;

    public int HttpTimeoutSeconds { get; set; } = 10;

    public int RetentionDays { get; set; } = 90;
}