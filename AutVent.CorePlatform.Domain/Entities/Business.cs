namespace AutVent.CorePlatform.Domain.Entities;

public class Business : BaseEntity
{
    public string BusinessName { get; set; } = string.Empty;
    public long StaffRangeId { get; set; }
    public virtual StaffRange StaffRange { get; set; } = null!;
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public long BusinessIndustryId { get; set; }
    public virtual BusinessIndustry BusinessIndustry { get; set; } = null!;
    public virtual ICollection<Store> Stores { get; set; } = [];
}
