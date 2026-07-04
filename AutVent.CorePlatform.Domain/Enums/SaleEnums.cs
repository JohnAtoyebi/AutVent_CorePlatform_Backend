namespace AutVent.CorePlatform.Domain.Enums;

public enum SalePaymentMethod
{
    Unknown = 0,
    Cash = 1,
    Transfer = 2,
    Pos = 3,
    Ussd = 4,
    PartPayment = 5
}

public enum SaleDiscountType
{
    Percentage = 1,
    Amount = 2
}
