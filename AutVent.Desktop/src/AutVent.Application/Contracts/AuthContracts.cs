namespace AutVent.Application.Contracts;

public sealed record LoginRequest(string Email, string Password, bool RememberMe);

public sealed record LoginResponse(
    long UserId,
    string FullName,
    string Email,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc,
    bool IsBusinessCreated);

/// <summary>Matches RefreshTokenResponse returned by POST /api/Authentication/refresh-token.</summary>
public sealed record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record AuthSessionDto(
    long UserId,
    string FullName,
    string Email,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc);

