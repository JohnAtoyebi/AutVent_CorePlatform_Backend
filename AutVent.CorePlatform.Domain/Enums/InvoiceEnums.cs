namespace AutVent.CorePlatform.Domain.Enums;

public enum InvoiceStatus
{
    Draft = 1,
    Sent = 2,
    Paid = 3,
    PartiallyPaid = 4,
    Overdue = 5,
    Cancelled = 6
}

public enum InvoicePaymentTerms
{
    Immediate = 1,
    Net7 = 2,
    Net14 = 3,
    Net30 = 4,
    Net60 = 5,
    Custom = 6
}
