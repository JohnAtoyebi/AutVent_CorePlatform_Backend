namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class ResendOtpResponse
{
    public string EmailAddress { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
}
