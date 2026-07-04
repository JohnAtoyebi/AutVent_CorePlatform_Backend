namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class InventorySummaryFilterRequest
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}
