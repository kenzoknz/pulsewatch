using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseWatch.Api.Data;
using PulseWatch.Api.DTOs;
using PulseWatch.Api.Models;

namespace PulseWatch.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public AdminController(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<AdminSystemStatsDto>> GetSystemStats()
    {
        var totalUsers = await _userManager.Users.CountAsync();
        var activeUsers = await _userManager.Users.CountAsync(u => u.IsActive);
        var totalWebsites = await _db.Websites.CountAsync();
        var activeWebsites = await _db.Websites.CountAsync(w => w.IsActive);

        var todayUtc = DateTime.UtcNow.Date;
        var totalUptimeChecksToday = await _db.UptimeChecks
            .CountAsync(c => c.CheckedAt >= todayUtc);

        var totalDowntimeEventsOpen = await _db.DowntimeEvents
            .CountAsync(e => e.EndedAt == null);

        return Ok(new AdminSystemStatsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            TotalWebsites = totalWebsites,
            ActiveWebsites = activeWebsites,
            TotalUptimeChecksToday = totalUptimeChecksToday,
            TotalDowntimeEventsOpen = totalDowntimeEventsOpen
        });
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserDto>>> GetUsers()
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();

        var websiteCounts = await _db.Websites
            .Where(w => userIds.Contains(w.UserId))
            .GroupBy(w => w.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var result = new List<AdminUserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new AdminUserDto
            {
                Id = user.Id,
                Username = user.UserName!,
                Email = user.Email!,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = roles,
                TotalWebsites = websiteCounts.GetValueOrDefault(user.Id, 0)
            });
        }

        return Ok(result);
    }

    [HttpPatch("users/{userId}/toggle-active")]
    public async Task<ActionResult<AdminUserDto>> ToggleUserActive(string userId, [FromBody] ToggleUserActiveDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(new { message = "User not found." });

        var isAdminTarget = await _userManager.IsInRoleAsync(user, AppRoles.Admin);
        if (isAdminTarget)
            return BadRequest(new { message = "Cannot deactivate an Admin account." });

        user.IsActive = dto.IsActive;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { message = "Update failed.", errors });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var totalWebsites = await _db.Websites.CountAsync(w => w.UserId == userId);

        return Ok(new AdminUserDto
        {
            Id = user.Id,
            Username = user.UserName!,
            Email = user.Email!,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = roles,
            TotalWebsites = totalWebsites
        });
    }

}
