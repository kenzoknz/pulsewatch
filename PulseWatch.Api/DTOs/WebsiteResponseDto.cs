namespace PulseWatch.Api.DTOs;

public class WebsiteResponseDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public int CheckIntervalMinutes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}