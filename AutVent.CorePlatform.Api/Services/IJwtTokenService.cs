using AutVent.CorePlatform.Domain.Entities;

namespace AutVent.CorePlatform.Api.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    (string Token, DateTime ExpiresAt) GenerateRefreshToken();
}
