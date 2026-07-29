namespace AutVent.CorePlatform.Domain.Entities;

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public long? BusinessId { get; set; }
    public virtual Business? Business { get; set; }
}
