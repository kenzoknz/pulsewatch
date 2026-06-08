using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PulseWatch.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PulseWatch.Api.Services;

/// <summary>
/// Cấu hình JWT từ appsettings.json.
/// </summary>
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

  
    public JwtTokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    /// Flow:
    /// 1. Tạo danh sách Claims (thông tin trong token)
    /// 2. Tạo signing key từ secret key
    /// 3. Tạo token với issuer, audience, claims, expiry, signature
    /// 4. Serialize token thành string
   
    public (string Token, DateTime ExpiresAt) GenerateToken(ApplicationUser user)
    {
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.Key)
        );

        
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiresInMinutes);

        
        var tokenDescriptor = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,      // Ai phát hành
            audience: _jwtOptions.Audience,   // Phát hành cho ai
            claims: claims,                    // Thông tin user
            expires: expiresAt,               // Khi nào hết hạn
            signingCredentials: credentials    // Chữ ký
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.WriteToken(tokenDescriptor);

        return (token, expiresAt);
    }
}