namespace AutVent.Domain.Enums;

/// <summary>Matches StockAdjustmentType enum on the AutVent API.</summary>
public enum StockAdjustmentType
{
    StockIn = 0,
    StockOut = 1
}

/// <summary>Matches StockAdjustmentReason enum on the AutVent API.</summary>
public enum StockAdjustmentReason
{
    Purchase = 0,
    Return = 1,
    Correction = 2,
    Damage = 3,
    Theft = 4,
    Transfer = 5,
    Other = 6
}
