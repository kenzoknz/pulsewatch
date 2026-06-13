using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseWatch.Api.Data;
using PulseWatch.Api.DTOs;
using PulseWatch.Api.Models;
using System.Security.Claims;

namespace PulseWatch.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public ProfileController(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Unauthorized.");
    }

    [HttpGet]
    public async Task<ActionResult<ProfileResponseDto>> GetProfile()
    {
        var userId = GetCurrentUserId();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var totalWebsites = await _db.Websites.CountAsync(w => w.UserId == userId);

        return Ok(new ProfileResponseDto
        {
            Id = user.Id,
            Username = user.UserName!,
            Email = user.Email!,
            CreatedAt = user.CreatedAt,
            TotalWebsites = totalWebsites
        });
    }

    [HttpPut]
    public async Task<ActionResult<ProfileResponseDto>> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = GetCurrentUserId();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var existingUser = await _userManager.FindByNameAsync(dto.Username);
        if (existingUser != null && existingUser.Id != userId)
            return BadRequest(new { message = "This username is already taken." });

        user.UserName = dto.Username;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { message = "Update failed.", errors });
        }

        var totalWebsites = await _db.Websites.CountAsync(w => w.UserId == userId);

        return Ok(new ProfileResponseDto
        {
            Id = user.Id,
            Username = user.UserName!,
            Email = user.Email!,
            CreatedAt = user.CreatedAt,
            TotalWebsites = totalWebsites
        });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = GetCurrentUserId();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { message = "Password change failed.", errors });
        }

        return Ok(new { message = "Password changed successfully." });
    }
}
