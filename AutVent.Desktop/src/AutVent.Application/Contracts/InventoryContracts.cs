using AutVent.Domain.Enums;

namespace AutVent.Application.Contracts;

/// <summary>
/// Adjusts stock on the server via POST /api/Inventory/store/{storeId}/product/{productId}/stock.
/// RemoteStoreId and RemoteProductId are the API's int64 identifiers.
/// </summary>
public sealed record AdjustInventoryCommand(
    Guid ProductId,
    long RemoteProductId,
    Guid StoreId,
    long RemoteStoreId,
    long Quantity,
    StockAdjustmentType AdjustmentType,
    StockAdjustmentReason Reason,
    string? Notes,
    string PerformedBy);

public sealed record InventoryItemDto(
    Guid InventoryRecordId,
    Guid ProductId,
    long RemoteProductId,
    long RemoteStoreId,
    string ProductName,
    string Sku,
    string CategoryName,
    decimal QuantityOnHand,
    decimal ReorderLevel,
    bool IsLowStock,
    bool IsActive);
