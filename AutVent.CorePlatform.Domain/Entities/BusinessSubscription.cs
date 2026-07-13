using AutVent.CorePlatform.Domain.Enums;

namespace AutVent.CorePlatform.Domain.Entities;

public class BusinessSubscription : BaseEntity
{
    public long BusinessId { get; set; }
    public long SubscriptionPlanId { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime TrialStartDate { get; set; }
    public DateTime TrialEndDate { get; set; }
    public DateTime? PlanStartDate { get; set; }
    public DateTime? PlanEndDate { get; set; }

    public virtual Business Business { get; set; } = null!;
    public virtual SubscriptionPlanDefinition SubscriptionPlan { get; set; } = null!;
}
