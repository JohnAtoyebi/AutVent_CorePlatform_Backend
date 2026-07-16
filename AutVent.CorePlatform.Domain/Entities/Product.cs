namespace AutVent.CorePlatform.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public long Quantity { get; set; }
    public long StoreId { get; set; }
    public virtual Store Store { get; set; } = null!;
    public long ProductCategoryId { get; set; }
    public virtual ProductCategory ProductCategory { get; set; } = null!;
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public string? CostPrice { get; set; }
    public string? CompareAtPrice { get; set; }
    public string? ProductImagesJson { get; set; }
    public bool? ProductVariantsEnabled { get; set; }
    public string? ProductVariantsJson { get; set; }
    public bool? AvailableOnPos { get; set; }
    public bool? AvailableOnAutShop { get; set; }
    public long? ReorderThreshold { get; set; }
    public bool? ApplyToAllStoreLocations { get; set; }
    public string? TagsJson { get; set; }
    public decimal? Weight { get; set; }
    public long? SupplierId { get; set; }
    public virtual Supplier? Supplier { get; set; }
}
