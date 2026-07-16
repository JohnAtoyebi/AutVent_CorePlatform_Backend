using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Domain.Enums;

public enum StockAdjustmentReason
{
    [Display(Name = "New Stock Received")]
    Purchase = 0,

    [Display(Name = "Customer Return")]
    Return = 1,

    [Display(Name = "Stock Correction")]
    Correction = 2,

    [Display(Name = "Damaged / Expired")]
    Damage = 3,

    [Display(Name = "Theft / Loss")]
    Theft = 4,

    [Display(Name = "Transfer")]
    Transfer = 5,

    [Display(Name = "Other")]
    Other = 6
}
