namespace PulseWatch.Api.DTOs;

public class UptimeCheckResponseDto
{
    public int Id { get; set; }

    public bool IsOnline { get; set; }

    public int? StatusCode { get; set; }

    public long ResponseTimeMs { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CheckedAt { get; set; }
}