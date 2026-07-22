using AutVent.Domain.Enums;

namespace AutVent.Application.Contracts;

/// <summary>One line item in a sale. RemoteProductId is the server's long id.</summary>
public sealed record SaleLineCommand(
    Guid ProductId,
    long RemoteProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal TaxAmount);

/// <summary>Full checkout command sent to POST /api/Pos/store/{storeId}/checkout.</summary>
public sealed record CompleteSaleCommand(
    Guid StoreId,
    long RemoteStoreId,
    long? RemoteCustomerId,
    IReadOnlyList<SaleLineCommand> Lines,
    decimal AmountPaid,
    PaymentMethod PaymentMethod,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal TaxAmount,
    string? Notes,
    DateTime? BalanceDueDate,
    decimal? ExpectedBalanceRemaining);

/// <summary>Lightweight confirmation returned after a successful checkout.</summary>
public sealed record CompletedSaleDto(
    long RemoteId,
    string SaleNumber,
    decimal TotalAmount,
    decimal ChangeAmount,
    string Status);
