namespace AutVent.CorePlatform.Domain.Entities;

public class Staff : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public long BusinessId { get; set; }
    public virtual Business Business { get; set; } = null!;
    public long RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;
    public bool HasAccessToAllStores { get; set; }
    public string? Notes { get; set; }
    public virtual ICollection<StaffStoreAccess> StoreAccess { get; set; } = new List<StaffStoreAccess>();
}
