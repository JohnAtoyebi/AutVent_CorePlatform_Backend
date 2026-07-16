using AutVent.CorePlatform.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class VerifyBillingTransactionRequest
{
    [Required]
    [MaxLength(100)]
    public string TransactionReference { get; init; } = string.Empty;

    [Required]
    [EnumDataType(typeof(TransactionVerificationStatus))]
    public TransactionVerificationStatus VerificationStatus { get; init; }

    [MaxLength(500)]
    public string? FailureReason { get; init; }
}
