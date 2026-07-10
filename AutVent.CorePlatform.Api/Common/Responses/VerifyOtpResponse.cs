namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class VerifyOtpResponse
{
    public string EmailAddress { get; init; } = string.Empty;
    public bool IsVerified { get; init; }
    public string AccessToken { get; init; } = string.Empty;
}
