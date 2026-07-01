namespace AutVent.CorePlatform.Domain.Entities;

public class Store : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public long StoreCategoryId { get; set; }
    public virtual StoreCategory StoreCategory { get; set; } = null!;
    public long BusinessId { get; set; }
    public virtual Business Business { get; set; } = null!;
}
