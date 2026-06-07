namespace PulseWatch.Api.DTOs;

public class DowntimeEventResponseDto
{
    public int Id { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public string? Reason { get; set; }

    public double? DurationMinutes { get; set; }
}