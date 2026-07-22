namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class CreateBusinessResponse
{
    public long BusinessId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Industry { get; init; } = string.Empty;
    public string StaffRange { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Website { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
}
