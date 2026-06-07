using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseWatch.Api.Data;
using PulseWatch.Api.DTOs;

namespace PulseWatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    public DashboardController(AppDbContext context)
    {
        _context = context;
    }
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        var websites = await _context.Websites
            .AsNoTracking()
            .Where(w => w.IsActive)
            .ToListAsync();

        var websiteIds = websites.Select(w => w.Id).ToList();

        var checks = websiteIds.Count == 0
            ? []
            : await _context.UptimeChecks
                .AsNoTracking()
                .Where(c => websiteIds.Contains(c.WebsiteId))
                .OrderByDescending(c => c.CheckedAt)
                .ToListAsync();

        var latestChecks = checks
            .GroupBy(c => c.WebsiteId)
            .Select(g => g.First())
            .ToList();

        var onlineWebsites = latestChecks.Count(c => c.IsOnline);
        var offlineWebsites = latestChecks.Count(c => !c.IsOnline);

        var averageResponseTime = latestChecks.Count == 0
            ? 0
            : Math.Round(latestChecks.Average(c => c.ResponseTimeMs), 2);

        var totalDowntimeEvents = await _context.DowntimeEvents
            .AsNoTracking()
            .CountAsync();

        var result = new DashboardSummaryDto
        {
            TotalWebsites = websites.Count,
            OnlineWebsites = onlineWebsites,
            OfflineWebsites = offlineWebsites,
            AverageResponseTimeMs = averageResponseTime,
            TotalDowntimeEvents = totalDowntimeEvents
        };

        return Ok(result);
    }
}