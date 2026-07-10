namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class SignInResponse
{
    public long UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string EmailAddress { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime RefreshTokenExpiresAtUtc { get; init; }
    public bool IsBusinessCreated { get; init; }
}
