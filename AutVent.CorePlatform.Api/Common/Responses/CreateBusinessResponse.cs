namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class CreateBusinessResponse
{
    public long BusinessId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Industry { get; init; } = string.Empty;
    public string StaffRange { get; init; } = string.Empty;
}
