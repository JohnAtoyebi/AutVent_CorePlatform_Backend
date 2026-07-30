namespace AutVent.CorePlatform.Api.Common.Responses;

/// <summary>
/// Dashboard response for business summary with comprehensive metrics
/// </summary>
public sealed class BusinessSummaryDashboardResponse
{
    /// <summary>
    /// Total number of businesses on the platform
    /// </summary>
    public long TotalBusinesses { get; init; }

    /// <summary>
    /// Number of active businesses
    /// </summary>
    public long ActiveBusinesses { get; init; }

    /// <summary>
    /// Number of suspended/inactive businesses
    /// </summary>
    public long SuspendedBusinesses { get; init; }

    /// <summary>
    /// Number of businesses in trial period
    /// </summary>
    public long TrialBusinesses { get; init; }

    /// <summary>
    /// Number of businesses with paid subscriptions
    /// </summary>
    public long PaidBusinesses { get; init; }

    /// <summary>
    /// Total number of stores across all businesses
    /// </summary>
    public long TotalStores { get; init; }

    /// <summary>
    /// Number of active stores
    /// </summary>
    public long ActiveStores { get; init; }

    /// <summary>
    /// Average number of stores per business
    /// </summary>
    public decimal AverageStoresPerBusiness { get; init; }
}
