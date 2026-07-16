using AutVent.CorePlatform.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class CreateBillingTransactionRequest
{
    [Required]
    [MaxLength(100)]
    public string TransactionReference { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? ProviderReference { get; init; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; init; }

    [MaxLength(10)]
    public string Currency { get; init; } = "NGN";

    [Required]
    [EnumDataType(typeof(BillingCycle))]
    public BillingCycle BillingCycle { get; init; }

    [Required]
    [Range(1, long.MaxValue)]
    public long SubscriptionPlanId { get; init; }
}
