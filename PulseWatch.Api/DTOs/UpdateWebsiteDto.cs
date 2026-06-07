using System.ComponentModel.DataAnnotations;

namespace PulseWatch.Api.DTOs;

public class UpdateWebsiteDto
{
    [Required(ErrorMessage = "Website name is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Website URL is required.")]
    [Url(ErrorMessage = "Invalid URL format.")]
    public string Url { get; set; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "Check interval must be between 1 and 1440 minutes.")]
    public int CheckIntervalMinutes { get; set; } = 5;

    public bool IsActive { get; set; } = true;
}