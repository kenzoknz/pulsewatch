namespace PulseWatch.Api.DTOs;

public class WebsiteStatsDto
{
    public int WebsiteId { get; set; }

    public string WebsiteName { get; set; } = string.Empty;

    public double UptimePercentage { get; set; }

    public double AverageResponseTimeMs { get; set; }

    public int TotalChecks { get; set; }

    public int OnlineChecks { get; set; }

    public int OfflineChecks { get; set; }

    public DateTime? LastCheckedAt { get; set; }

    public bool? CurrentStatus { get; set; }
}
