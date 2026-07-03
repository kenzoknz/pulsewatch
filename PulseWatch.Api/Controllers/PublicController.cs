using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseWatch.Api.Data;
using PulseWatch.Api.DTOs;

namespace PulseWatch.Api.Controllers;

/// <summary>
/// Unauthenticated public endpoints for status badges and status pages.
/// No [Authorize] attribute — these are intentionally public.
/// </summary>
[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<PublicController> _logger;

    public PublicController(AppDbContext db, ILogger<PublicController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET /api/public/websites/{id}/status
    /// <summary>Returns public uptime statistics for a website that has IsPublic = true.</summary>
    [HttpGet("websites/{id:int}/status")]
    public async Task<ActionResult<PublicWebsiteStatsDto>> GetPublicStatus(int id)
    {
        var website = await _db.Websites
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id && w.IsPublic);

        if (website == null)
            return NotFound(new { message = "This status page is not available or does not exist." });

        var checks = await _db.UptimeChecks
            .AsNoTracking()
            .Where(c => c.WebsiteId == id)
            .OrderByDescending(c => c.CheckedAt)
            .Take(500) // Compute stats from up to 500 recent checks
            .ToListAsync();

        var totalChecks = checks.Count;
        var onlineChecks = checks.Count(c => c.IsOnline);
        var offlineChecks = totalChecks - onlineChecks;
        var uptimePercentage = totalChecks == 0 ? 0 : (double)onlineChecks / totalChecks * 100;
        var avgResponseTime = totalChecks == 0 ? 0 : checks.Average(c => c.ResponseTimeMs);

        // Last 50 check data points for the frontend chart
        var recentChecks = checks.Take(50).Select(c => new PublicCheckDataPointDto
        {
            IsOnline = c.IsOnline,
            ResponseTimeMs = c.ResponseTimeMs,
            CheckedAt = c.CheckedAt
        }).ToList();

        // Recent downtime events (max 10)
        var recentDowntimeEvents = await _db.DowntimeEvents
            .AsNoTracking()
            .Where(e => e.WebsiteId == id)
            .OrderByDescending(e => e.StartedAt)
            .Take(10)
            .Select(e => new PublicDowntimeEventDto
            {
                StartedAt = e.StartedAt,
                EndedAt = e.EndedAt,
                Reason = e.Reason,
                DurationMinutes = e.EndedAt.HasValue
                    ? (e.EndedAt.Value - e.StartedAt).TotalMinutes
                    : null
            })
            .ToListAsync();

        return Ok(new PublicWebsiteStatsDto
        {
            Id = website.Id,
            Name = website.Name,
            Url = website.Url,
            IsOnline = website.IsOnline,
            LastStatusCode = website.LastStatusCode,
            LastResponseTimeMs = website.LastResponseTimeMs,
            LastCheckedAt = website.LastCheckedAt,
            UptimePercentage = Math.Round(uptimePercentage, 2),
            AverageResponseTimeMs = Math.Round(avgResponseTime, 2),
            TotalChecks = totalChecks,
            OnlineChecks = onlineChecks,
            OfflineChecks = offlineChecks,
            RecentChecks = recentChecks,
            RecentDowntimeEvents = recentDowntimeEvents
        });
    }

    // GET /api/public/websites/{id}/badge
    /// <summary>Returns a shields.io-style SVG badge for embedding in README files.</summary>
    [HttpGet("websites/{id:int}/badge")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)] // Cache badge for 60 seconds
    public async Task<IActionResult> GetBadge(int id)
    {
        var website = await _db.Websites
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id && w.IsPublic);

        if (website == null)
            return NotFound();

        // Calculate uptime percentage
        var checksCount = await _db.UptimeChecks
            .AsNoTracking()
            .CountAsync(c => c.WebsiteId == id);

        double uptimePct = 0;
        if (checksCount > 0)
        {
            var onlineCount = await _db.UptimeChecks
                .AsNoTracking()
                .CountAsync(c => c.WebsiteId == id && c.IsOnline);
            uptimePct = Math.Round((double)onlineCount / checksCount * 100, 1);
        }

        // Choose colors and label based on status
        string statusLabel;
        string statusColor;
        string leftColor = "555555";

        if (website.IsOnline == true)
        {
            statusLabel = $"up  {uptimePct}%";
            statusColor = "2ea44f"; // green
        }
        else if (website.IsOnline == false)
        {
            statusLabel = "down";
            statusColor = "cf222e"; // red
        }
        else
        {
            statusLabel = "unknown";
            statusColor = "6e7681"; // grey
        }

        var leftLabel = Uri.EscapeDataString(website.Name.Length > 20 ? website.Name[..20] + "…" : website.Name);
        var rightLabel = Uri.EscapeDataString(statusLabel);

        var svg = BuildBadgeSvg(website.Name, statusLabel, leftColor, statusColor);

        return Content(svg, "image/svg+xml");
    }

    private static string BuildBadgeSvg(string leftText, string rightText, string leftColor, string rightColor)
    {
        // Approximate character width at 11px font ~= 6.5px per char, + 10px padding each side
        int leftWidth = Math.Max(60, leftText.Length * 7 + 20);
        int rightWidth = Math.Max(50, rightText.Length * 7 + 20);
        int totalWidth = leftWidth + rightWidth;

        int leftCenter = leftWidth / 2;
        int rightCenter = leftWidth + rightWidth / 2;

        return $@"<svg xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" width=""{totalWidth}"" height=""20"" role=""img"" aria-label=""{leftText}: {rightText}"">
  <title>{leftText}: {rightText}</title>
  <linearGradient id=""s"" x2=""0"" y2=""100%"">
    <stop offset=""0"" stop-color=""#bbb"" stop-opacity="".1""/>
    <stop offset=""1"" stop-opacity="".1""/>
  </linearGradient>
  <clipPath id=""r"">
    <rect width=""{totalWidth}"" height=""20"" rx=""3"" fill=""#fff""/>
  </clipPath>
  <g clip-path=""url(#r)"">
    <rect width=""{leftWidth}"" height=""20"" fill=""#{leftColor}""/>
    <rect x=""{leftWidth}"" width=""{rightWidth}"" height=""20"" fill=""#{rightColor}""/>
    <rect width=""{totalWidth}"" height=""20"" fill=""url(#s)""/>
  </g>
  <g fill=""#fff"" text-anchor=""middle"" font-family=""DejaVu Sans,Verdana,Geneva,sans-serif"" font-size=""110"">
    <text aria-hidden=""true"" x=""{leftCenter * 10}"" y=""150"" fill=""#010101"" fill-opacity="".3"" transform=""scale(.1)"" textLength=""{(leftWidth - 16) * 10}"">{leftText}</text>
    <text x=""{leftCenter * 10}"" y=""140"" transform=""scale(.1)"" fill=""#fff"" textLength=""{(leftWidth - 16) * 10}"">{leftText}</text>
    <text aria-hidden=""true"" x=""{rightCenter * 10}"" y=""150"" fill=""#010101"" fill-opacity="".3"" transform=""scale(.1)"" textLength=""{(rightWidth - 16) * 10}"">{rightText}</text>
    <text x=""{rightCenter * 10}"" y=""140"" transform=""scale(.1)"" fill=""#fff"" textLength=""{(rightWidth - 16) * 10}"">{rightText}</text>
  </g>
</svg>";
    }
}
