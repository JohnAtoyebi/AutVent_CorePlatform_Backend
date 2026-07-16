namespace AutVent.CorePlatform.Domain.Enums;

public enum SubscriptionPlan
{
    Starter = 1,
    Growth = 2,
    Enterprise = 3
}

public enum SubscriptionStatus
{
    Trial = 1,
    Active = 2,
    Expired = 3,
    Cancelled = 4
}

public enum BillingCycle
{
    Monthly = 1,
    Annual = 2
}

public enum TransactionVerificationStatus
{
    Pending = 1,
    Verified = 2,
    Failed = 3,
    Abandoned = 4
}
