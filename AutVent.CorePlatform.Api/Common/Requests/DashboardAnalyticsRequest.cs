namespace AutVent.CorePlatform.Api.Common.Requests;

/// <summary>
/// Request for dashboard analytics with date range filtering
/// </summary>
public sealed class DashboardAnalyticsRequest
{
    /// <summary>
    /// Start date for analytics period (inclusive)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// End date for analytics period (inclusive)
    /// </summary>
    public DateTime? ToDate { get; set; }
}
