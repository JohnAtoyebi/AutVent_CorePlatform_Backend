namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class WaitlistResponse
{
    public long Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string EmailAddress { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? BusinessType { get; init; }
    public string? Notes { get; init; }
    public bool IsContacted { get; init; }
    public DateTime JoinedAt { get; init; }
}
