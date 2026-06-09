namespace PulseWatch.Api.Models;

public class Website
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    public int CheckIntervalSeconds { get; set; } = 300;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastCheckedAt { get; set; }

    public DateTime NextCheckAt { get; set; } = DateTime.UtcNow;

    public bool? IsOnline { get; set; }
    public int? LastStatusCode { get; set; }
    public long? LastResponseTimeMs { get; set; }

    public List<UptimeCheck> UptimeChecks { get; set; } = new();
}