namespace PulseWatch.Api.DTOs;

public class PublicDowntimeEventDto
{
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Reason { get; set; }
    public double? DurationMinutes { get; set; }
}

/// <summary>Slim public DTO returned by /api/public/websites/{id}/status.
/// Does NOT expose owner identity or internal configuration details.</summary>
public class PublicWebsiteStatsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    public bool? IsOnline { get; set; }
    public int? LastStatusCode { get; set; }
    public long? LastResponseTimeMs { get; set; }
    public DateTime? LastCheckedAt { get; set; }

    public double UptimePercentage { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public int TotalChecks { get; set; }
    public int OnlineChecks { get; set; }
    public int OfflineChecks { get; set; }

    /// <summary>Last 50 uptime check data points for charting.</summary>
    public List<PublicCheckDataPointDto> RecentChecks { get; set; } = new();

    /// <summary>Most recent downtime events (max 10).</summary>
    public List<PublicDowntimeEventDto> RecentDowntimeEvents { get; set; } = new();
}

public class PublicCheckDataPointDto
{
    public bool IsOnline { get; set; }
    public long ResponseTimeMs { get; set; }
    public DateTime CheckedAt { get; set; }
}
