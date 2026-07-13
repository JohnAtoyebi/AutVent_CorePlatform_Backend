using AutVent.CorePlatform.Domain.Enums;

namespace AutVent.CorePlatform.Domain.Entities;

public class SubscriptionPlanDefinition : BaseEntity
{
    public SubscriptionPlan Plan { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public int? MaxStores { get; set; }
    public int? MaxStaff { get; set; }
    public int? MaxProducts { get; set; }
}
