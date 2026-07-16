namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class ValidateReferralCodeResponse
{
    public string ReferralCode { get; init; } = string.Empty;
    public bool IsValid { get; init; }
    public string ReferrerName { get; init; } = string.Empty;
}
