namespace AutVent.CorePlatform.Domain.Entities;

public class RolePermission : BaseEntity
{
    public long RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;
    public long PermissionId { get; set; }
    public virtual Permission Permission { get; set; } = null!;
}
