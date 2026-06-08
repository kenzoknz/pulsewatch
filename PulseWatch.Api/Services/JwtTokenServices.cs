using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PulseWatch.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace PulseWatch.Api.Services;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; } = 60;
}

public class JwtTokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly UserManager<ApplicationUser> _userManager;
  
    public JwtTokenService(IOptions<JwtOptions> jwtOptions, UserManager<ApplicationUser> userManager)
    {
        _jwtOptions  = jwtOptions.Value;
        _userManager = userManager; 
    }

    /// Flow:
    /// 1. Tạo danh sách Claims (thông tin trong token)
    /// 2. Tạo signing key từ secret key
    /// 3. Tạo token với issuer, audience, claims, expiry, signature
    /// 4. Serialize token thành string
   
    public async Task<(string Token, DateTime ExpiresAt)> GenerateTokenAsync(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email,          user.Email!),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt   = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiresInMinutes);

        var tokenDescriptor = new JwtSecurityToken(
            issuer:             _jwtOptions.Issuer,
            audience:           _jwtOptions.Audience,
            claims:             claims,
            expires:            expiresAt,
            signingCredentials: credentials
        );

        var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

        return (token, expiresAt);
    }
}