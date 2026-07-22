namespace AutVent.Domain.Entities;

public sealed class Product
{
    public Guid Id { get; set; }

    /// <summary>Server-side integer id from the API (productId).</summary>
    public long RemoteId { get; set; }

    public long RemoteStoreId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public decimal CostPrice { get; set; }

    public decimal TaxRate { get; set; }

    public bool IsActive { get; set; }

    public bool AvailableOnPos { get; set; }

    public int QuantityOnHand { get; set; }

    public int ReorderThreshold { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
