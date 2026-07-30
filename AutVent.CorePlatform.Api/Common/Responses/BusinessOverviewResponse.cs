namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class BusinessOverviewResponse
{
    public long TotalBusiness { get; init; }
    public long ActiveBusiness { get; init; }
    public long TrialBusiness { get; init; }
    public long SuspendedBusiness { get; init; }
}
