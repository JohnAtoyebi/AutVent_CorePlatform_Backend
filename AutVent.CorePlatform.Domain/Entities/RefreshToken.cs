namespace AutVent.CorePlatform.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public DateTime DateExpired { get; set; }
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
}
