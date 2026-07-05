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
    public DateTime? LastDeepCheckAt { get; set; }

    // --- Public Status Page ---
    /// <summary>When true, the website stats are accessible publicly without authentication.</summary>
    public bool IsPublic { get; set; } = false;

    // --- Advanced Check Options ---
    /// <summary>HTTP method used for uptime checks. Allowed values: "GET", "HEAD", "POST".</summary>
    public string HttpMethod { get; set; } = "GET";

    /// <summary>Optional JSON string containing custom request headers, e.g. {"X-API-Key":"abc"}.</summary>
    public string? CustomHeadersJson { get; set; }

    /// <summary>Optional keyword that must appear in the response body for the site to be considered online.
    /// Not evaluated when HttpMethod is "HEAD". If set and keyword is absent (even on 200 OK), site is marked DOWN.</summary>
    public string? ResponseBodyKeyword { get; set; }
}