using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseWatch.Api.Data;
using PulseWatch.Api.DTOs;
using PulseWatch.Api.Models;

namespace PulseWatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebsitesController : ControllerBase
{
    private readonly AppDbContext _context;
    public WebsitesController(AppDbContext context)
    {
        _context = context;
    }


    [HttpGet]
    public async Task<ActionResult<List<WebsiteResponseDto>>> GetWebsites()
    {
        var websites = await _context.Websites
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => ToResponseDto(w))
            .ToListAsync();

        return Ok(websites);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WebsiteResponseDto>> GetWebsite(int id)
    {
        var website = await _context.Websites.FindAsync(id);

        if (website == null)
        {
            return NotFound();
        }

        return Ok(ToResponseDto(website));
    }

    [HttpPost]
    public async Task<ActionResult<WebsiteResponseDto>> CreateWebsite(CreateWebsiteDto dto)
    {
        var website = new Website
        {
            Name = dto.Name.Trim(),
            Url = dto.Url.Trim(),
            CheckIntervalMinutes = dto.CheckIntervalMinutes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Websites.Add(website);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetWebsite), new { id = website.Id }, ToResponseDto(website));
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWebsite(int id, UpdateWebsiteDto dto)
    {
        var website = await _context.Websites.FindAsync(id);
        if (website == null)
        {
            return NotFound();
        }
        website.Name = dto.Name.Trim();
        website.Url = dto.Url.Trim();
        website.CheckIntervalMinutes = dto.CheckIntervalMinutes;
        website.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }
    [HttpDelete("{id}")] //Soft delete - set IsActive to false
    public async Task<IActionResult> DeleteWebsite(int id)
    {
        var website = await _context.Websites.FindAsync(id);

        if (website == null)
        {
            return NotFound();
        }
        website.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }
    [HttpGet("{id}/checks")]
    public async Task<ActionResult<List<UptimeCheckResponseDto>>> GetWebsiteChecks(int id)
    {
        var websiteExists = await _context.Websites.AnyAsync(w => w.Id == id);
        if (!websiteExists)
        {
            return NotFound();
        }
        var checks = await _context.UptimeChecks
            .Where(c => c.WebsiteId == id)
            .OrderByDescending(c => c.CheckedAt)
            .Take(50)
            .Select(c => ToCheckResponseDto(c))
            .ToListAsync();
        return Ok(checks);
    }
    [HttpGet("{id}/stats")]
    public async Task<ActionResult<WebsiteStatsDto>> GetWebsiteStats(int id)
    {
        var website = await _context.Websites.FindAsync(id);

        if (website == null) return NotFound();
        var checks = await _context.UptimeChecks
            .Where(c => c.WebsiteId == id)
            .ToListAsync();
        var totalChecks = checks.Count;
        var onlineChecks = checks.Count(c => c.IsOnline);
        var offlineChecks = checks.Count(c => !c.IsOnline);

        var latestCheck = checks
                        .OrderByDescending(c => c.CheckedAt)
                        .FirstOrDefault();
        var result = new WebsiteStatsDto
        {
            WebsiteId = website.Id,
            WebsiteName = website.Name,
            TotalChecks = totalChecks,
            OnlineChecks = onlineChecks,
            OfflineChecks = offlineChecks,
            UptimePercentage = totalChecks == 0 ? 0 : Math.Round((double)onlineChecks / totalChecks * 100, 2),
            AverageResponseTimeMs = totalChecks == 0 ? 0 : Math.Round(checks.Average(c => c.ResponseTimeMs), 2),
            LastCheckedAt = latestCheck?.CheckedAt,
            CurrentStatus = latestCheck?.IsOnline
        };

        return Ok(result);
    }
    [HttpGet("{id}/downtime-events")]
    public async Task<ActionResult<List<DowntimeEventResponseDto>>> GetDowntimeEvents(int id)
    {
        var websiteExists = await _context.Websites.AnyAsync(w => w.Id == id);

        if (!websiteExists)
        {
            return NotFound();
        }

        var events = await _context.DowntimeEvents
            .Where(e => e.WebsiteId == id) //website ID sort decending
            .OrderByDescending(e => e.StartedAt)
            .Select(e => new DowntimeEventResponseDto
            {
                Id = e.Id,
                StartedAt = e.StartedAt,
                EndedAt = e.EndedAt,
                Reason = e.Reason,
                DurationMinutes = e.EndedAt == null
                    ? null
                    : Math.Round((e.EndedAt.Value - e.StartedAt).TotalMinutes, 2)
            })
            .ToListAsync();

        return Ok(events);
    }
    private static WebsiteResponseDto ToResponseDto(Website website)
    {
        return new WebsiteResponseDto
        {
            Id = website.Id,
            Name = website.Name,
            Url = website.Url,
            CheckIntervalMinutes = website.CheckIntervalMinutes,
            IsActive = website.IsActive,
            CreatedAt = website.CreatedAt
        };
    }
    private static UptimeCheckResponseDto ToCheckResponseDto(UptimeCheck check)
    {
        return new UptimeCheckResponseDto
        {
            Id = check.Id,
            IsOnline = check.IsOnline,
            StatusCode = check.StatusCode,
            ResponseTimeMs = check.ResponseTimeMs,
            ErrorMessage = check.ErrorMessage,
            CheckedAt = check.CheckedAt
        };
    }

}