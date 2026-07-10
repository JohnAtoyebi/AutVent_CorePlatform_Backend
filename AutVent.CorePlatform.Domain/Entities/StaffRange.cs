namespace AutVent.CorePlatform.Domain.Entities;

public class StaffRange : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public virtual ICollection<Business> Businesses { get; set; } = [];
}
