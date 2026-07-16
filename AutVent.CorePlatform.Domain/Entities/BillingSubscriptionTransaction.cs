using AutVent.CorePlatform.Domain.Enums;

namespace AutVent.CorePlatform.Domain.Entities;

public class BillingSubscriptionTransaction : BaseEntity
{
    public long BusinessId { get; set; }
    public long SubscriptionPlanId { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "NGN";
    public BillingCycle BillingCycle { get; set; }
    public TransactionVerificationStatus VerificationStatus { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public virtual Business Business { get; set; } = null!;
    public virtual SubscriptionPlanDefinition SubscriptionPlan { get; set; } = null!;
}
