namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
