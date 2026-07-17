namespace AutVent.CorePlatform.Domain.Entities;

/// <summary>
/// Join table that tracks which product categories are visible to a given business.
/// Both system-default categories (adopted on business creation) and custom categories
/// created by the business are represented here.
/// </summary>
public class BusinessProductCategory : BaseEntity
{
    public long BusinessId { get; set; }
    public virtual Business Business { get; set; } = null!;

    public long ProductCategoryId { get; set; }
    public virtual ProductCategory ProductCategory { get; set; } = null!;
}
