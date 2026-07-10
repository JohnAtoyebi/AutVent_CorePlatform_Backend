using System.ComponentModel.DataAnnotations;
using AutVent.CorePlatform.Domain.Enums;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class CreateSaleItemRequest
{
    [Required]
    public long ProductId { get; init; }

    [Range(1, long.MaxValue)]
    public long Quantity { get; init; }

    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; init; }
}

public sealed class CreateSaleRequest
{
    public long? CustomerId { get; init; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal AmountPaid { get; init; }

    [Required]
    [EnumDataType(typeof(SalePaymentMethod))]
    public SalePaymentMethod PaymentMethod { get; init; }

    [EnumDataType(typeof(SaleDiscountType))]
    public SaleDiscountType? DiscountType { get; init; }

    [Range(0, double.MaxValue)]
    public decimal DiscountValue { get; init; }

    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }

    public DateTime? BalanceDueDate { get; init; }

    public decimal? ExpectedBalanceRemaining { get; init; }

    [Required]
    public List<CreateSaleItemRequest> Items { get; init; } = [];
}
