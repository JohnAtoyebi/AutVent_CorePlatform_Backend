namespace AutVent.CorePlatform.Domain.Entities;

public class StaffStoreAccess : BaseEntity
{
    public long StaffId { get; set; }
    public virtual Staff Staff { get; set; } = null!;
    public long StoreId { get; set; }
    public virtual Store Store { get; set; } = null!;
}
