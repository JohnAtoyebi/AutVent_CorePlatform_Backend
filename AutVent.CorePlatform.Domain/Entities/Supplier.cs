namespace AutVent.CorePlatform.Domain.Entities;

public class Supplier : BaseEntity
{
    public long BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}
