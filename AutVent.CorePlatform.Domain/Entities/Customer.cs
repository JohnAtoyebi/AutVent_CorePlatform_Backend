namespace AutVent.CorePlatform.Domain.Entities;

public class Customer : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public long StoreId { get; set; }
    public virtual Store Store { get; set; } = null!;
}
