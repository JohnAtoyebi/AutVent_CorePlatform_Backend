namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class BusinessProductResponse
{
    public long ProductId { get; init; }
    public long StoreId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Price { get; init; } = string.Empty;
    public long Quantity { get; init; }
    public string ProductCategory { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
