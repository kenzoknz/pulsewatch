namespace PulseWatch.Api.Models;

public class UptimeCheck
{
    public int Id { get; set; }

    public int WebsiteId { get; set; }

    public Website Website { get; set; } = null!;

    public bool IsOnline { get; set; }

    public int? StatusCode { get; set; } // nullable

    public long ResponseTimeMs { get; set; }

    public string? ErrorMessage { get; set; } // error message

    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}