using System.ComponentModel.DataAnnotations;

namespace PulseWatch.Api.DTOs;

public class CreateWebsiteDto
{
    [Required(ErrorMessage = "Website name is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Website URL is required.")]
    public string Url { get; set; } = string.Empty;

    [Range(60, 86400, ErrorMessage = "Check interval must be between 60 and 86400 seconds (1 minute to 24 hours).")]
    public int CheckIntervalSeconds { get; set; } = 300;

    // --- Public Status Page ---
    public bool IsPublic { get; set; } = false;

    // --- Advanced Check Options ---
    [RegularExpression("^(GET|HEAD|POST)$", ErrorMessage = "HttpMethod must be GET, HEAD, or POST.")]
    public string HttpMethod { get; set; } = "GET";

    [StringLength(2048, ErrorMessage = "CustomHeadersJson must be 2048 characters or less.")]
    public string? CustomHeadersJson { get; set; }

    [StringLength(500, ErrorMessage = "ResponseBodyKeyword must be 500 characters or less.")]
    public string? ResponseBodyKeyword { get; set; }
}
