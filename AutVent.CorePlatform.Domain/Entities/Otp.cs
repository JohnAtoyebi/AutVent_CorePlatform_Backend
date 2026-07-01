namespace AutVent.CorePlatform.Domain.Entities;

public class Otp : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsExpired { get; set; }
    public DateTime DateExpired { get; set; }
}
