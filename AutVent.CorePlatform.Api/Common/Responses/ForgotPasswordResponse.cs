namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class ForgotPasswordResponse
{
    public string EmailAddress { get; init; } = string.Empty;
    public DateTime TokenExpiresAtUtc { get; init; }
}
