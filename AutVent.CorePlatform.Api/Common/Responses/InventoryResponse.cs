namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class InventorySummaryResponse
{
    public long StoreId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public long ProductSoldCount { get; init; }
    public decimal ProductSoldPercentageIncrease { get; init; }
    public int SalesCount { get; init; }
    public decimal SalesCountPercentageIncrease { get; init; }
    public int NewCustomerCount { get; init; }
    public decimal NewCustomerPercentageIncrease { get; init; }
    public int LowStockCount { get; init; }
    public decimal LowStockPercentageIncrease { get; init; }
    public int OutOfStockCount { get; init; }
    public decimal OutOfStockPercentageIncrease { get; init; }
}

public sealed class InventoryItemResponse
{
    public long ProductId { get; init; }
    public long StoreId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public long Quantity { get; init; }
    public long? ReorderThreshold { get; init; }
    public bool IsLowStock { get; init; }
    public bool IsActive { get; init; }
    public string ProductCategory { get; init; } = string.Empty;
    public string? CostPrice { get; init; }
}
