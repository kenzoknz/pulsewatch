using System.ComponentModel.DataAnnotations;

namespace PulseWatch.Api.DTOs;

public class UpdateWebsiteDto
{
    [Required(ErrorMessage = "Website name is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Website URL is required.")]
    public string Url { get; set; } = string.Empty;

    [Range(60, 86400, ErrorMessage = "Check interval must be between 60 and 86400 seconds.")]
    public int CheckIntervalSeconds { get; set; } = 300;

    public bool IsActive { get; set; } = true;
}