namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class UserProfileResponse
{
    public long Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string EmailAddress { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? ReferralCode { get; init; }
    public bool IsActive { get; init; }
    public string? ProfilePhotoUrl { get; init; }
    public DateTimeOffset MemberSince { get; init; }
}
