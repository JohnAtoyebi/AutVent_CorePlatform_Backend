namespace AutVent.CorePlatform.Domain.Entities;

public class WaitlistEntry : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? BusinessType { get; set; }
    public string? Notes { get; set; }
    public bool IsContacted { get; set; }
}
