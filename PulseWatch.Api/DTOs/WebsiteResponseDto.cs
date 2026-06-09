namespace PulseWatch.Api.DTOs;

public class WebsiteResponseDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public int CheckIntervalSeconds { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool? IsOnline { get; set; }
    public int? LastStatusCode { get; set; }
    public long? LastResponseTimeMs { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public DateTime NextCheckAt { get; set; }
}