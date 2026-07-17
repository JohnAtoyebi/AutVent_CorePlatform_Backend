namespace AutVent.CorePlatform.Domain.Entities;

public class ProductCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>True for system-seeded categories; false for business-created custom categories.</summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// The business that first coined this category name (null for seeded defaults).
    /// Not used for visibility — <see cref="BusinessProductCategory"/> controls that.
    /// </summary>
    public long? CreatedByBusinessId { get; set; }
    public virtual Business? CreatedByBusiness { get; set; }

    public virtual ICollection<BusinessProductCategory> BusinessMappings { get; set; } = [];
}
