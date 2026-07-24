namespace AutVent.CorePlatform.Domain.Enums;

public enum AuditAction
{
    UserSignedIn = 0,
    UserPasswordChanged = 1,
    UserEmailChanged = 2,
    StockAdjusted = 3,
    ProductDeleted = 4,
    StaffDeleted = 5,
    StaffDeactivated = 6,
    StoreDeactivated = 7,
    CustomerDeleted = 8,
    SupplierDeleted = 9,
    SaleVoided = 10,
    BankAccountDeleted = 11,
    BusinessCreated = 12,
    UserProfileCreated = 13,
    UserProfileUpdated = 14,
    BusinessUpdated = 15,
    StoreCreated = 16,
    StoreUpdated = 17,
}

