namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class RegisterUserResponse
{
    public long UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string EmailAddress { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? ReferralCode { get; init; }
    public bool IsActive { get; init; }
    public DateTime OtpExpiresAtUtc { get; init; }
}
