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

    [HttpPost("users")]
    public async Task<ActionResult<AdminUserDto>> CreateUser([FromBody] CreateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existingByEmail = await _userManager.FindByEmailAsync(dto.Email);
        if (existingByEmail != null)
            return BadRequest(new { message = "Email already in use." });

        var existingByUsername = await _userManager.FindByNameAsync(dto.Username);
        if (existingByUsername != null)
            return BadRequest(new { message = "Username already taken." });

        var user = new ApplicationUser
        {
            UserName = dto.Username,
            Email = dto.Email,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors.Select(e => e.Description);
            return BadRequest(new { message = "Failed to create user.", errors });
        }

        var role = string.IsNullOrWhiteSpace(dto.Role) ? AppRoles.User : dto.Role;
        await _userManager.AddToRoleAsync(user, role);

        var roles = await _userManager.GetRolesAsync(user);
        return CreatedAtAction(nameof(GetUsers), new { }, new AdminUserDto
        {
            Id = user.Id,
            Username = user.UserName!,
            Email = user.Email!,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = roles,
            TotalWebsites = 0
        });
    }

    [HttpPut("users/{userId}")]
    public async Task<ActionResult<AdminUserDto>> UpdateUser(string userId, [FromBody] UpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(new { message = "User not found." });

        var isAdminTarget = await _userManager.IsInRoleAsync(user, AppRoles.Admin);

        if (dto.Username != null)
        {
            var existingByUsername = await _userManager.FindByNameAsync(dto.Username);
            if (existingByUsername != null && existingByUsername.Id != userId)
                return BadRequest(new { message = "Username already taken." });
            user.UserName = dto.Username;
        }

        if (dto.Email != null)
        {
            var existingByEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingByEmail != null && existingByEmail.Id != userId)
                return BadRequest(new { message = "Email already in use." });
            user.Email = dto.Email;
        }

        if (dto.IsActive.HasValue)
        {
            if (isAdminTarget && dto.IsActive == false)
                return BadRequest(new { message = "Cannot deactivate an Admin account." });
            user.IsActive = dto.IsActive.Value;
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = updateResult.Errors.Select(e => e.Description);
            return BadRequest(new { message = "Update failed.", errors });
        }

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passResult = await _userManager.ResetPasswordAsync(user, token, dto.Password);
            if (!passResult.Succeeded)
            {
                var errors = passResult.Errors.Select(e => e.Description);
                return BadRequest(new { message = "Password update failed.", errors });
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.Role) && !isAdminTarget)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, dto.Role);
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

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(new { message = "User not found." });

        var isAdminTarget = await _userManager.IsInRoleAsync(user, AppRoles.Admin);
        if (isAdminTarget)
            return BadRequest(new { message = "Cannot delete an Admin account." });

        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            var errors = deleteResult.Errors.Select(e => e.Description);
            return BadRequest(new { message = "Delete failed.", errors });
        }

        return NoContent();
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
