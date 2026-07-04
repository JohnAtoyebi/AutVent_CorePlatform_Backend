namespace AutVent.CorePlatform.Domain.Entities;

public class PasswordResetToken : BaseEntity
{
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public DateTime DateExpired { get; set; }
    public bool IsUsed { get; set; }
}
