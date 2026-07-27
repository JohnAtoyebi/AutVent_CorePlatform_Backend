namespace AutVent.CorePlatform.Domain.Entities;

public class Business : BaseEntity
{
    public string BusinessName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public long StaffRangeId { get; set; }
    public virtual StaffRange StaffRange { get; set; } = null!;
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public long BusinessIndustryId { get; set; }
    public virtual BusinessIndustry BusinessIndustry { get; set; } = null!;
    public virtual ICollection<Store> Stores { get; set; } = [];
}
