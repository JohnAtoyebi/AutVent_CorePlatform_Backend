using AutVent.CorePlatform.Domain.Enums;

namespace AutVent.CorePlatform.Domain.Entities;

public class AuditLog : BaseEntity
{
    public long UserId { get; set; }
    public long? BusinessId { get; set; }
    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public long? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Business? Business { get; set; }
}
