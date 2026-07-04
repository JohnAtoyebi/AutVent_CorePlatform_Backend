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
}
