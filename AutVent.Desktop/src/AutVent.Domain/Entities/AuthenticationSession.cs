namespace AutVent.Domain.Entities;

public sealed class AuthenticationSession
{
    public Guid Id { get; set; }

    public long UserId { get; set; }

    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime AccessTokenExpiresAtUtc { get; set; }

    public DateTime RefreshTokenExpiresAtUtc { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
