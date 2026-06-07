using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseWatch.Api.Data;
using PulseWatch.Api.DTOs;
using PulseWatch.Api.Models;
using PulseWatch.Api.Services;

namespace PulseWatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebsitesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UptimeCheckerService _uptimeCheckerService;

    public WebsitesController(AppDbContext context, UptimeCheckerService uptimeCheckerService)
    {
        _context = context;
        _uptimeCheckerService = uptimeCheckerService;
    }


    [HttpPost("bulk")]
    public async Task<ActionResult<BulkCreateWebsiteResultDto>> BulkCreateWebsites(BulkCreateWebsitesDto dto)
    {
        var result = new BulkCreateWebsiteResultDto();

        if (dto.Urls == null || dto.Urls.Count == 0)
        {
            result.Summary = new BulkCreateWebsiteSummaryDto { Total = 0 };
            return Ok(result);
        }

        var maxInterval = 1440;
        var minInterval = 1;
        var checkInterval = Math.Clamp(dto.DefaultCheckIntervalMinutes, minInterval, maxInterval);
        var strategy = string.IsNullOrWhiteSpace(dto.NameStrategy) ? "auto" : dto.NameStrategy.ToLowerInvariant();

        // Load existing URLs for duplicate check
        var existingUrls = await _context.Websites
            .Select(w => w.Url)
            .ToListAsync();
        var existingUrlSet = new HashSet<string>(existingUrls, StringComparer.OrdinalIgnoreCase);

        // Track seen normalized URLs in this batch
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Step 1: Normalize all URLs and classify
        var normalizedEntries = new List<(string Original, string? Normalized, string? Error)>();

        foreach (var rawUrl in dto.Urls)
        {
            if (string.IsNullOrWhiteSpace(rawUrl))
            {
                continue;
            }

            if (!TryNormalizeUrl(rawUrl, out var normalizedUrl))
            {
                normalizedEntries.Add((rawUrl, null, "Invalid website URL or domain"));
                continue;
            }

            normalizedEntries.Add((rawUrl, normalizedUrl, null));
        }

        result.Summary.Total = normalizedEntries.Count;

        // Step 2: Process valid URLs
        var websitesToCreate = new List<Website>();

        foreach (var entry in normalizedEntries)
        {
            // Invalid URL
            if (entry.Normalized == null)
            {
                result.Failed.Add(new BulkWebsiteErrorDto
                {
                    Url = entry.Original,
                    Reason = entry.Error ?? "Invalid URL"
                });
                continue;
            }

            // Duplicate in this batch
            if (!seenUrls.Add(entry.Normalized))
            {
                result.Skipped.Add(new BulkWebsiteErrorDto
                {
                    Url = entry.Original,
                    Reason = "Duplicate URL in request"
                });
                continue;
            }

            // Already exists in DB
            if (existingUrlSet.Contains(entry.Normalized))
            {
                result.Skipped.Add(new BulkWebsiteErrorDto
                {
                    Url = entry.Original,
                    Reason = "Website already exists"
                });
                continue;
            }

            // Resolve name
            var name = await ResolveNameAsync(entry.Normalized, strategy);

            var website = new Website
            {
                Name = name,
                Url = entry.Normalized,
                CheckIntervalMinutes = checkInterval,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            websitesToCreate.Add(website);
            existingUrlSet.Add(entry.Normalized);
        }

        // Step 3: Save all new websites
        if (websitesToCreate.Count > 0)
        {
            _context.Websites.AddRange(websitesToCreate);
            await _context.SaveChangesAsync();
        }

        // Step 4: Build response
        result.Created = websitesToCreate.Select(w => ToResponseDto(w)).ToList();
        result.Summary.Created = result.Created.Count;
        result.Summary.Skipped = result.Skipped.Count;
        result.Summary.Failed = result.Failed.Count;

        return Ok(result);
    }

    private async Task<string> ResolveNameAsync(string url, string strategy)
    {
        // Try to get title from page
        if (strategy is "title" or "auto")
        {
            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                    if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
                    {
                        var html = await response.Content.ReadAsStringAsync();
                        var titleMatch = System.Text.RegularExpressions.Regex.Match(
                            html,
                            @"<title[^>]*>(.*?)</title>",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase
                                | System.Text.RegularExpressions.RegexOptions.Singleline);

                        if (titleMatch.Success)
                        {
                            var title = System.Net.WebUtility.HtmlDecode(titleMatch.Groups[1].Value.Trim());
                            if (!string.IsNullOrWhiteSpace(title) && title.Length <= 200)
                            {
                                return title;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fall through to hostname fallback
            }

            if (strategy == "title")
            {
                // Strategy "title" with no title found: fallback to hostname
            }
        }

        // Fallback: derive name from hostname
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var host = uri.Host;
            if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            {
                host = host[4..];
            }
            return host;
        }

        return url;
    }

    [HttpGet]
    public async Task<ActionResult<List<WebsiteResponseDto>>> GetWebsites()
    {
        var websites = await _context.Websites
            .Where(w => w.IsActive)
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
        if (!TryNormalizeUrl(dto.Url, out var normalizedUrl))
        {
            return BadRequest("Invalid website URL or domain.");
        }

        var website = new Website
        {
            Name = dto.Name.Trim(),
            Url = normalizedUrl,
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
        if (!TryNormalizeUrl(dto.Url, out var normalizedUrl))
        {
            return BadRequest("Invalid website URL or domain.");
        }

        website.Name = dto.Name.Trim();
        website.Url = normalizedUrl;
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
    public async Task<ActionResult<PagedResponseDto<UptimeCheckResponseDto>>> GetWebsiteChecks(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var websiteExists = await _context.Websites.AnyAsync(w => w.Id == id);
        if (!websiteExists)
        {
            return NotFound();
        }

        var query = _context.UptimeChecks
            .Where(c => c.WebsiteId == id)
            .OrderByDescending(c => c.CheckedAt);

        var totalItems = await query.CountAsync();

        var checks = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => ToCheckResponseDto(c))
            .ToListAsync();

        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)pageSize);

        return Ok(new PagedResponseDto<UptimeCheckResponseDto>
        {
            Items = checks,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        });
    }
    [HttpPost("{id}/checks/run")]
    public async Task<ActionResult<UptimeCheckResponseDto>> RunWebsiteCheck(int id)
    {
        var website = await _context.Websites.FindAsync(id);

        if (website == null)
        {
            return NotFound();
        }

        var lastCheck = await _context.UptimeChecks
            .Where(c => c.WebsiteId == id)
            .OrderByDescending(c => c.CheckedAt)
            .FirstOrDefaultAsync();

        var openDowntime = await _context.DowntimeEvents
            .FirstOrDefaultAsync(d => d.WebsiteId == id && d.EndedAt == null);

        var check = await _uptimeCheckerService.CheckWebsiteAsync(website);

        if ((lastCheck == null || lastCheck.IsOnline) && !check.IsOnline)
        {
            _context.DowntimeEvents.Add(new DowntimeEvent
            {
                WebsiteId = website.Id,
                StartedAt = check.CheckedAt,
                Reason = check.ErrorMessage ?? $"Status code: {check.StatusCode}"
            });
        }

        if (openDowntime != null && check.IsOnline)
        {
            openDowntime.EndedAt = check.CheckedAt;
        }

        _context.UptimeChecks.Add(check);
        await _context.SaveChangesAsync();

        return Ok(ToCheckResponseDto(check));
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
    private static bool TryNormalizeUrl(string url, out string normalizedUrl)
    {
        normalizedUrl = string.Empty;

        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var trimmedUrl = url.Trim();
        var urlWithScheme = trimmedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? trimmedUrl
            : $"https://{trimmedUrl}";

        if (!Uri.TryCreate(urlWithScheme, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(uri.Host) || !uri.Host.Contains('.'))
        {
            return false;
        }

        normalizedUrl = uri.ToString();
        return true;
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