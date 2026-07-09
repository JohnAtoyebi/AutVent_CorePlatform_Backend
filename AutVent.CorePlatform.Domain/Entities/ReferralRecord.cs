namespace AutVent.CorePlatform.Domain.Entities;

public class ReferralRecord : BaseEntity
{
    public long ReferrerId { get; set; }
    public virtual User Referrer { get; set; } = null!;
    public long ReferredUserId { get; set; }
    public virtual User ReferredUser { get; set; } = null!;
    public string ReferralCode { get; set; } = string.Empty;
}
