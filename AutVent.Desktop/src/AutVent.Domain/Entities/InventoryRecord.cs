namespace AutVent.Domain.Entities;

public sealed class InventoryRecord
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Guid StoreId { get; set; }

    /// <summary>Server-side productId from InventoryItemResponse.</summary>
    public long RemoteProductId { get; set; }

    /// <summary>Server-side storeId from InventoryItemResponse.</summary>
    public long RemoteStoreId { get; set; }

    public decimal QuantityOnHand { get; set; }

    public decimal ReorderLevel { get; set; }

    public bool IsLowStock { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
