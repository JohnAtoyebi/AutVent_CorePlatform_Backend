namespace AutVent.Domain.Enums;

/// <summary>Matches SalePaymentMethod enum on the AutVent API.</summary>
public enum PaymentMethod
{
    Unknown = 0,
    Cash = 1,
    Transfer = 2,
    Pos = 3,
    Ussd = 4,
    PartPayment = 5
}

/// <summary>Matches SaleDiscountType enum on the AutVent API.</summary>
public enum DiscountType
{
    Percentage = 0,
    Amount = 1
}
