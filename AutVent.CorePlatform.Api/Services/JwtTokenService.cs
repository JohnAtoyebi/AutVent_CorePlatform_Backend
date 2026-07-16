using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutVent.CorePlatform.Api.Common.Security;
using AutVent.CorePlatform.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace AutVent.CorePlatform.Api.Services;

public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public string GenerateAccessToken(User user) => GenerateAccessTokenWithExpiry(user).Token;

    public (string Token, DateTime ExpiresAt) GenerateAccessTokenWithExpiry(User user)
    {
        var jwt = configuration.GetSection("Jwt").Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT settings are missing.");

        if (string.IsNullOrWhiteSpace(jwt.Key) || string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience))
            throw new InvalidOperationException("JWT settings are invalid.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(jwt.ExpiryMinutes <= 0 ? 60 : jwt.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.EmailAddress),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public (string Token, DateTime ExpiresAt) GenerateRefreshToken()
    {
        var jwt = configuration.GetSection("Jwt").Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT settings are missing.");

        var expiryDays = jwt.RefreshTokenExpiryDays <= 0 ? 7 : jwt.RefreshTokenExpiryDays;
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return (token, DateTime.UtcNow.AddDays(expiryDays));
    }
}
