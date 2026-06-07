namespace PulseWatch.Api.DTOs;

public class DashboardSummaryDto
{
    public int TotalWebsites { get; set; }

    public int OnlineWebsites { get; set; }

    public int OfflineWebsites { get; set; }

    public double AverageResponseTimeMs { get; set; }

    public int TotalDowntimeEvents { get; set; }
}
