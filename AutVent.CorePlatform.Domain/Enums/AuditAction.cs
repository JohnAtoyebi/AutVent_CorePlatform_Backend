namespace AutVent.CorePlatform.Domain.Enums;

public enum AuditAction
{
    // Auth
    UserSignedIn,
    UserPasswordChanged,
    UserEmailChanged,

    // Inventory
    StockAdjusted,

    // Destructive / Deactivation
    ProductDeleted,
    StaffDeleted,
    StaffDeactivated,
    StoreDeactivated,
    CustomerDeleted,
    SupplierDeleted,
    SaleVoided,
    BankAccountDeleted,

    // Business creation (one-time critical onboarding event)
    BusinessCreated,
}

