namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class UpdateProfileResponse
{
    public long Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
}
