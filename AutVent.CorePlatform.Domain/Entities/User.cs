namespace AutVent.CorePlatform.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? ReferralCode { get; set; }
    public bool IsEmailVerified { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public long? RoleId { get; set; }
    public virtual Role? Role { get; set; }
}
