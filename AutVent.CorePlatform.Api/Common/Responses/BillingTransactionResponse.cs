namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class BillingTransactionResponse
{
    public long Id { get; init; }
    public long BusinessId { get; init; }
    public long SubscriptionPlanId { get; init; }
    public string SubscriptionPlanName { get; init; } = string.Empty;
    public string TransactionReference { get; init; } = string.Empty;
    public string? ProviderReference { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string BillingCycle { get; init; } = string.Empty;
    public string VerificationStatus { get; init; } = string.Empty;
    public string? FailureReason { get; init; }
    public DateTime? VerifiedAt { get; init; }
    public DateTime DateCreated { get; init; }
    public DateTime? DateUpdated { get; init; }
}
