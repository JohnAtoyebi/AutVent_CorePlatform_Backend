namespace AutVent.CorePlatform.Domain.Enums;

public enum NotificationType
{
    // Sales
    SaleCompleted = 1,
    SalePartPayment = 2,

    // Inventory
    LowStock = 3,
    OutOfStock = 4,
    StockTransferCompleted = 5,

    // Invoices
    InvoiceSent = 6,
    InvoicePaid = 7,
    InvoiceOverdue = 8,

    // Staff
    StaffAdded = 9,
    StaffRemoved = 10,

    // Subscription
    SubscriptionExpiringSoon = 11,
    SubscriptionExpired = 12,
    SubscriptionUpgraded = 13,

    // System
    General = 99
}

public enum NotificationChannel
{
    InApp = 1
}
