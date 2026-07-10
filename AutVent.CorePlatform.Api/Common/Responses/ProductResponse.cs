namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class ProductVariantResponse
{
    public string? Variant { get; init; }
    public string? Sku { get; init; }
    public string? Price { get; init; }
    public long? Quantity { get; init; }
}

public sealed class ProductResponse
{
    public long ProductId { get; init; }
    public long StoreId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Price { get; init; } = string.Empty;
    public long Quantity { get; init; }
    public string ProductCategory { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Sku { get; init; }
    public string? Barcode { get; init; }
    public string? CostPrice { get; init; }
    public string? CompareAtPrice { get; init; }
    public List<string>? ProductImages { get; init; }
    public bool? ProductVariantsEnabled { get; init; }
    public List<ProductVariantResponse>? ProductVariants { get; init; }
    public bool ActiveProduct { get; init; }
    public bool? AvailableOnPos { get; init; }
    public bool? AvailableOnAutShop { get; init; }
    public long? ReorderThreshold { get; init; }
    public bool? ApplyToAllStoreLocations { get; init; }
    public List<string>? Tags { get; init; }
    public decimal? Weight { get; init; }
    public string? Supplier { get; init; }
    public decimal? ProfitMargin { get; init; }
}
