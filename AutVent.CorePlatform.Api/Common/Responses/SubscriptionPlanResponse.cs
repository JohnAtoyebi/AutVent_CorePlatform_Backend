namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class SubscriptionPlanResponse
{
    public long Id { get; init; }
    public string Plan { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal MonthlyPrice { get; init; }
    public decimal AnnualPrice { get; init; }
    public int? MaxStores { get; init; }
    public int? MaxStaff { get; init; }
    public int? MaxProducts { get; init; }
    public bool IsActive { get; init; }
}
