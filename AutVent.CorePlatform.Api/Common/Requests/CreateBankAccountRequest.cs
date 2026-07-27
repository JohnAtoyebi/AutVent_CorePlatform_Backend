using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class CreateBankAccountRequest
{
    [Required]
    [MaxLength(200)]
    public string BankName { get; init; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string AccountNumber { get; init; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string AccountName { get; init; } = string.Empty;

    [MaxLength(20)]
    public string? SortCode { get; init; }

    public bool IsDefault { get; init; }
}
