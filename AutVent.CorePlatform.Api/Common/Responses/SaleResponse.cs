using AutVent.CorePlatform.Domain.Enums;

namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class SaleItemResponse
{
    public long SaleItemId { get; init; }
    public long ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public long Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}

public sealed class SaleResponse
{
    public long SaleId { get; init; }
    public string SaleNumber { get; init; } = string.Empty;
    public long StoreId { get; init; }
    public long? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public decimal SubTotal { get; init; }
    public SaleDiscountType? DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal AmountPaid { get; init; }
    public decimal BalanceRemaining { get; init; }
    public decimal ChangeAmount { get; init; }
    public SalePaymentMethod PaymentMethod { get; init; }
    public string? Notes { get; init; }
    public List<SaleItemResponse> Items { get; init; } = [];
    public DateTime SaleDate { get; init; }
}
