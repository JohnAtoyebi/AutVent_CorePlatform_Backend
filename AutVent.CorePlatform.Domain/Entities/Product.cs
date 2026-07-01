namespace AutVent.CorePlatform.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public long Quantity { get; set; }
    public long ProductCategoryId { get; set; }
    public virtual ProductCategory ProductCategory { get; set; } = null!;
}
