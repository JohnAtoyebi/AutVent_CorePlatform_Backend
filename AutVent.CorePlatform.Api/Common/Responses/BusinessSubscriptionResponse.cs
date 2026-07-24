namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class BusinessSubscriptionResponse
{
    public long Id { get; init; }
    public long BusinessId { get; init; }
    public long SubscriptionPlanId { get; init; }
    public string SubscriptionPlanName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime TrialStartDate { get; init; }
    public DateTime TrialEndDate { get; init; }
    public DateTime? PlanStartDate { get; init; }
    public DateTime? PlanEndDate { get; init; }
    public DateTime DateCreated { get; init; }
    public DateTime? DateUpdated { get; init; }
}
