using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PulseWatch.Api.Data;
using PulseWatch.Api.DTOs;
using PulseWatch.Api.Models;
using PulseWatch.Api.Services;
using System.Security.Claims;

namespace PulseWatch.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly AppDbContext _db;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwtTokenService,
        AppDbContext db)
    {
        _userManager     = userManager;
        _jwtTokenService = jwtTokenService;
        _db              = db;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
    {
        var existingEmail = await _userManager.FindByEmailAsync(dto.Email);
        if (existingEmail != null)
            return BadRequest(new { message = "This email has already been registered." });

        var existingUsername = await _userManager.FindByNameAsync(dto.Username);
        if (existingUsername != null)
            return BadRequest(new { message = "This username is already taken." });

        var user = new ApplicationUser
        {
            UserName       = dto.Username,
            Email          = dto.Email,
            EmailConfirmed = true,
            CreatedAt      = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { message = "Registration failed.", errors });
        }

        await _userManager.AddToRoleAsync(user, AppRoles.User);

        var (token, expiresAt) = await _jwtTokenService.GenerateTokenAsync(user);

        return Ok(new AuthResponseDto
        {
            AccessToken = token,
            ExpiresAt   = expiresAt,
            User        = new UserDto { Id = user.Id, Email = user.Email!, Username = user.UserName! }
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        var isEmail = dto.EmailOrUsername.Contains('@');
        var user = isEmail
            ? await _userManager.FindByEmailAsync(dto.EmailOrUsername)
            : await _userManager.FindByNameAsync(dto.EmailOrUsername);

        if (user == null)
            return Unauthorized(new { message = "Invalid email/username or password." });

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!isPasswordValid)
            return Unauthorized(new { message = "Invalid email/username or password." });

        var (token, expiresAt) = await _jwtTokenService.GenerateTokenAsync(user);

        return Ok(new AuthResponseDto
        {
            AccessToken = token,
            ExpiresAt   = expiresAt,
            User        = new UserDto { Id = user.Id, Email = user.Email!, Username = user.UserName! }
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Invalid token." });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        return Ok(new UserDto { Id = user.Id, Email = user.Email!, Username = user.UserName! });
    }
}