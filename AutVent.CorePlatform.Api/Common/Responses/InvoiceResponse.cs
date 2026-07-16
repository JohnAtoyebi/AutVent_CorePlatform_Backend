using AutVent.CorePlatform.Domain.Enums;

namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class InvoiceItemResponse
{
    public long Id { get; init; }
    public long ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public long Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Discount { get; init; }
    public decimal LineTotal { get; init; }
}

public sealed class InvoiceResponse
{
    public long Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public long StoreId { get; init; }
    public long? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public DateTime IssueDate { get; init; }
    public DateTime DueDate { get; init; }
    public InvoicePaymentTerms PaymentTerms { get; init; }
    public decimal SubTotal { get; init; }
    public SaleDiscountType? DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal VatRate { get; init; }
    public decimal VatAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal AmountPaid { get; init; }
    public decimal BalanceRemaining { get; init; }
    public SalePaymentMethod PaymentMethod { get; init; }
    public InvoiceStatus Status { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<InvoiceItemResponse> Items { get; init; } = new();
}
