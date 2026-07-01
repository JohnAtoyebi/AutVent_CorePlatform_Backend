namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class CategoryResponse
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
