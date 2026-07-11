namespace AutVent.CorePlatform.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? ReferralCode { get; set; }
    public bool IsEmailVerified { get; set; }
}
