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
}