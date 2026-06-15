using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseWatch.Api.Data;
using PulseWatch.Api.DTOs;
using PulseWatch.Api.Models;
using PulseWatch.Api.Services;

namespace PulseWatch.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WebsitesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UptimeCheckerService _checker;
    private readonly ILogger<WebsitesController> _logger;
    private readonly IDeepCheckService _deepCheck;

    public WebsitesController(AppDbContext db, UptimeCheckerService checker, ILogger<WebsitesController> logger, IDeepCheckService deepCheck)
    {
        _db = db;
        _checker = checker;
        _logger = logger;
        _deepCheck = deepCheck;
    }

    // GET /api/websites (paged)
    [HttpGet]
    public async Task<ActionResult<PagedResponseDto<WebsiteResponseDto>>> GetWebsites(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var userId = GetCurrentUserId();
        var query = _db.Websites
                    .Where(w => w.UserId == userId)
                    .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(w =>
                w.Name.Contains(search) || w.Url.Contains(search));
        }

        var totalItems = await query.CountAsync();

        var websites = await query
            .OrderBy(w => w.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = websites.Select(MapToDto).ToList();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        return Ok(new PagedResponseDto<WebsiteResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        });
    }

    // GET /api/websites/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<WebsiteResponseDto>> GetWebsite(int id)
    {
        var userId = GetCurrentUserId();
        var website = await _db.Websites
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (website == null) return NotFound();

        return Ok(MapToDto(website));
    }

    // POST /api/websites
    [HttpPost]
    public async Task<ActionResult<WebsiteResponseDto>> CreateWebsite([FromBody] CreateWebsiteDto dto)
    {
        var userId = GetCurrentUserId(); 

        var website = new Website
        {
            UserId = userId,
            Name = dto.Name,
            Url = dto.Url,
            CheckIntervalSeconds = dto.CheckIntervalSeconds,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,

            NextCheckAt = DateTime.UtcNow,
            LastCheckedAt = null,
            IsOnline = null
        };


        _db.Websites.Add(website);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetWebsite), new { id = website.Id }, MapToDto(website));
    }

    // GET /api/websites/{id}/stats
    [HttpGet("{id:int}/stats")]
    public async Task<ActionResult<WebsiteStatsDto>> GetWebsiteStats(int id)
    {
        var userId = GetCurrentUserId();
        var website = await _db.Websites
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
        if (website == null) return NotFound();

        var checks = await _db.UptimeChecks
            .AsNoTracking()
            .Where(c => c.WebsiteId == id)
            .OrderByDescending(c => c.CheckedAt)
            .ToListAsync();

        var totalChecks = checks.Count;
        var onlineChecks = checks.Count(c => c.IsOnline);
        var offlineChecks = totalChecks - onlineChecks;
        var uptimePercentage = totalChecks == 0 ? 0 : (double)onlineChecks / totalChecks * 100;
        var avgResponseTime = totalChecks == 0 ? 0 : checks.Average(c => c.ResponseTimeMs);
        var lastCheck = checks.FirstOrDefault();

        return Ok(new WebsiteStatsDto
        {
            WebsiteId = id,
            WebsiteName = website.Name,
            UptimePercentage = Math.Round(uptimePercentage, 2),
            AverageResponseTimeMs = Math.Round(avgResponseTime, 2),
            TotalChecks = totalChecks,
            OnlineChecks = onlineChecks,
            OfflineChecks = offlineChecks,
            LastCheckedAt = lastCheck?.CheckedAt,
            CurrentStatus = lastCheck?.IsOnline
        });
    }

    // GET /api/websites/{id}/checks
    [HttpGet("{id:int}/checks")]
    public async Task<ActionResult<PagedResponseDto<UptimeCheckResponseDto>>> GetWebsiteChecks(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        var website = await _db.Websites
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
        if (website == null) return NotFound();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _db.UptimeChecks
            .AsNoTracking()
            .Where(c => c.WebsiteId == id);

        var totalItems = await query.CountAsync();

        var checks = await query
            .OrderByDescending(c => c.CheckedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new UptimeCheckResponseDto
            {
                Id = c.Id,
                IsOnline = c.IsOnline,
                StatusCode = c.StatusCode,
                ResponseTimeMs = c.ResponseTimeMs,
                ErrorMessage = c.ErrorMessage,
                CheckedAt = c.CheckedAt
            })
            .ToListAsync();

        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        return Ok(new PagedResponseDto<UptimeCheckResponseDto>
        {
            Items = checks,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        });
    }

    // GET /api/websites/{id}/downtime-events
    [HttpGet("{id:int}/downtime-events")]
    public async Task<ActionResult<List<DowntimeEventResponseDto>>> GetDowntimeEvents(int id)
    {
        var userId = GetCurrentUserId();
        var website = await _db.Websites
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
        if (website == null) return NotFound();

        var events = await _db.DowntimeEvents
            .AsNoTracking()
            .Where(e => e.WebsiteId == id)
            .OrderByDescending(e => e.StartedAt)
            .Select(e => new DowntimeEventResponseDto
            {
                Id = e.Id,
                StartedAt = e.StartedAt,
                EndedAt = e.EndedAt,
                Reason = e.Reason,
                DurationMinutes = e.EndedAt.HasValue
                    ? (e.EndedAt.Value - e.StartedAt).TotalMinutes
                    : null
            })
            .ToListAsync();

        return Ok(events);
    }

    // PUT /api/websites/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<WebsiteResponseDto>> UpdateWebsite(int id, [FromBody] UpdateWebsiteDto dto)
    {
        var userId = GetCurrentUserId();

        var website = await _db.Websites
        .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (website == null) return NotFound();

        website.Name = dto.Name;
        website.Url = dto.Url;
        website.IsActive = dto.IsActive;

        if (website.CheckIntervalSeconds != dto.CheckIntervalSeconds)
        {
            website.CheckIntervalSeconds = dto.CheckIntervalSeconds;

            var baseTime = website.LastCheckedAt ?? DateTime.UtcNow;
            website.NextCheckAt = baseTime.AddSeconds(dto.CheckIntervalSeconds);

            if (website.NextCheckAt < DateTime.UtcNow)
            {
                website.NextCheckAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();

        return Ok(MapToDto(website));
    }

    // DELETE /api/websites/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteWebsite(int id)
    {
        var userId = GetCurrentUserId();
        var website = await _db.Websites
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
        if (website == null) return NotFound();

        _db.Websites.Remove(website);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // POST /api/websites/{id}/check
    [HttpPost("{id:int}/check")]
    public async Task<ActionResult<UptimeCheckResponseDto>> RunCheck(int id)
    {
        var userId = GetCurrentUserId();
        var website = await _db.Websites
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
        if (website == null) return NotFound();

        var result = await _checker.CheckWebsiteAsync(website);

        website.IsOnline = result.IsOnline;
        website.LastStatusCode = result.StatusCode;
        website.LastResponseTimeMs = result.ResponseTimeMs;
        website.LastCheckedAt = result.CheckedAt;
        website.NextCheckAt = result.CheckedAt.AddSeconds(website.CheckIntervalSeconds);

        _db.UptimeChecks.Add(result);
        await _db.SaveChangesAsync();

        return Ok(new UptimeCheckResponseDto
        {
            Id = result.Id,
            IsOnline = result.IsOnline,
            StatusCode = result.StatusCode,
            ResponseTimeMs = result.ResponseTimeMs,
            ErrorMessage = result.ErrorMessage,
            CheckedAt = result.CheckedAt
        });
    }

    // POST /api/websites/bulk
    [HttpPost("bulk")]
    public async Task<ActionResult<BulkCreateWebsiteResultDto>> BulkCreate([FromBody] BulkCreateWebsitesDto dto)
    {
        if (dto.Urls == null || dto.Urls.Count == 0)
        {
            return BadRequest(new BulkCreateWebsiteResultDto
            {
                Summary = new BulkCreateWebsiteSummaryDto { Total = 0 }
            });
        }

        var userId = GetCurrentUserId();
        var summary = new BulkCreateWebsiteSummaryDto { Total = dto.Urls.Count };
        var created = new List<WebsiteResponseDto>();
        var skipped = new List<BulkWebsiteErrorDto>();
        var failed = new List<BulkWebsiteErrorDto>();

        foreach (var url in dto.Urls)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                skipped.Add(new BulkWebsiteErrorDto { Url = url, Reason = "Empty URL." });
                summary.Skipped++;
                continue;
            }

            try
            {
                var website = new Website
                {
                    UserId = userId,
                    Name = dto.NameStrategy == "auto"
                        ? new Uri(url.Trim()).Host
                        : url.Trim(),
                    Url = url.Trim(),
                    CheckIntervalSeconds = dto.DefaultCheckIntervalSeconds,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    NextCheckAt = DateTime.UtcNow,
                    IsOnline = null
                };

                _db.Websites.Add(website);
                await _db.SaveChangesAsync();

                created.Add(MapToDto(website));
                summary.Created++;
            }
            catch (Exception ex)
            {
                failed.Add(new BulkWebsiteErrorDto { Url = url, Reason = ex.Message });
                summary.Failed++;
            }
        }

        return Ok(new BulkCreateWebsiteResultDto
        {
            Summary = summary,
            Created = created,
            Skipped = skipped,
            Failed = failed
        });
    }

    // POST /api/websites/bulk-check
    [HttpPost("bulk-check")]
    public async Task<ActionResult<CheckAllResponseDto>> BulkCheck([FromBody] BulkCheckRequestDto dto)
    {
        if (dto.WebsiteIds == null || dto.WebsiteIds.Count == 0)
            return BadRequest("No website IDs provided.");

        var userId = GetCurrentUserId();
        var websites = await _db.Websites
            .Where(w => w.UserId == userId && dto.WebsiteIds.Contains(w.Id))
            .ToListAsync();

        var summary = new CheckAllResponseDto
        {
            Total = dto.WebsiteIds.Count,
            Skipped = dto.WebsiteIds.Count - websites.Count
        };

        foreach (var website in websites)
        {
            try
            {
                var checkResult = await _checker.CheckWebsiteAsync(website);
                _db.UptimeChecks.Add(checkResult);
                await _db.SaveChangesAsync();

                summary.Results.Add(new BulkCheckItemResultDto
                {
                    WebsiteId = website.Id,
                    Name = website.Name,
                    IsOnline = checkResult.IsOnline,
                    StatusCode = checkResult.StatusCode,
                    ResponseTimeMs = checkResult.ResponseTimeMs,
                    ErrorMessage = checkResult.ErrorMessage
                });

                if (checkResult.IsOnline) summary.Success++;
                else summary.Failed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking website {WebsiteId}", website.Id);
                summary.Results.Add(new BulkCheckItemResultDto
                {
                    WebsiteId = website.Id,
                    Name = website.Name,
                    IsOnline = false,
                    ErrorMessage = ex.Message
                });
                summary.Failed++;
            }
        }

        return Ok(summary);
    }

    // POST /api/websites/bulk-delete
    [HttpPost("bulk-delete")]
    public async Task<ActionResult<BulkDeleteResponseDto>> BulkDelete([FromBody] BulkDeleteRequestDto dto)
    {
        if (dto.WebsiteIds == null || dto.WebsiteIds.Count == 0)
            return BadRequest("No website IDs provided.");

        var userId = GetCurrentUserId();
        var websites = await _db.Websites
            .Where(w => w.UserId == userId && dto.WebsiteIds.Contains(w.Id))
            .ToListAsync();

        var count = websites.Count;
        _db.Websites.RemoveRange(websites);
        await _db.SaveChangesAsync();

        return Ok(new BulkDeleteResponseDto { DeletedCount = count });
    }

    // POST /api/websites/check-all
    [HttpPost("check-all")]
    public async Task<ActionResult<CheckAllResponseDto>> CheckAll()
    {
        var userId = GetCurrentUserId();
        var websites = await _db.Websites
            .Where(w => w.UserId == userId && w.IsActive)
            .ToListAsync();

        var summary = new CheckAllResponseDto
        {
            Total = websites.Count
        };

        foreach (var website in websites)
        {
            try
            {
                var checkResult = await _checker.CheckWebsiteAsync(website);
                _db.UptimeChecks.Add(checkResult);
                await _db.SaveChangesAsync();

                summary.Results.Add(new BulkCheckItemResultDto
                {
                    WebsiteId = website.Id,
                    Name = website.Name,
                    IsOnline = checkResult.IsOnline,
                    StatusCode = checkResult.StatusCode,
                    ResponseTimeMs = checkResult.ResponseTimeMs,
                    ErrorMessage = checkResult.ErrorMessage
                });

                if (checkResult.IsOnline) summary.Success++;
                else summary.Failed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking website {WebsiteId}", website.Id);
                summary.Results.Add(new BulkCheckItemResultDto
                {
                    WebsiteId = website.Id,
                    Name = website.Name,
                    IsOnline = false,
                    ErrorMessage = ex.Message
                });
                summary.Failed++;
            }
        }

        return Ok(summary);
    }

    // POST /api/websites/delete-all
    [HttpPost("delete-all")]
    public async Task<ActionResult<DeleteAllResponseDto>> DeleteAll()
    {
        var userId = GetCurrentUserId();
        var deletedCount = await _db.Websites
            .Where(w => w.UserId == userId)
            .ExecuteDeleteAsync();

        return Ok(new DeleteAllResponseDto { DeletedCount = deletedCount });
    }

    // POST /api/websites/{id}/deep-check
    [HttpPost("{id:int}/deep-check")]
    public async Task<IActionResult> RunDeepCheck(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var website = await _db.Websites
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, cancellationToken);

        if (website == null)
            return NotFound();

        const int cooldownMinutes = 2;
        if (website.LastDeepCheckAt.HasValue)
        {
            var elapsed = DateTime.UtcNow - website.LastDeepCheckAt.Value;
            if (elapsed.TotalMinutes < cooldownMinutes)
            {
                var remaining = Math.Ceiling(cooldownMinutes - elapsed.TotalMinutes);
                return StatusCode(429, new { message = $"Please wait {remaining} minutes to try again." });
            }
        }

        var result = await _deepCheck.RunCheckAsync(website.Url, cancellationToken);

        website.LastDeepCheckAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(result);
    }

    private static WebsiteResponseDto MapToDto(Website website)
    {
        return new WebsiteResponseDto
        {
            Id = website.Id,
            Name = website.Name,
            Url = website.Url,
            CheckIntervalSeconds = website.CheckIntervalSeconds,
            IsActive = website.IsActive,
            CreatedAt = website.CreatedAt,
            IsOnline = website.IsOnline,
            LastStatusCode = website.LastStatusCode,
            LastResponseTimeMs = website.LastResponseTimeMs,
            LastCheckedAt = website.LastCheckedAt,
            NextCheckAt = website.NextCheckAt
        };
    }
    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Unauthorized.");
    }
}