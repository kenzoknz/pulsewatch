namespace PulseWatch.Api.DTOs;

public class DeepCheckResponseDto
{
    public bool IsOnline { get; set; }
    public long ResponseTimeMs { get; set; }
    public string? ScreenshotBase64 { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PageTitle { get; set; }
}