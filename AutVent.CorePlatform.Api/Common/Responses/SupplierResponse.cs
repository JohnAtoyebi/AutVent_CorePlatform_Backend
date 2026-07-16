namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class SupplierResponse
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public DateTime CreatedAt { get; init; }
}
