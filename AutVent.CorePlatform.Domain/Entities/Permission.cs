namespace AutVent.CorePlatform.Domain.Entities;

public class Permission : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Group { get; set; } = string.Empty;
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
