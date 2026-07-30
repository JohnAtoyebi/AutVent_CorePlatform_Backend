namespace AutVent.CorePlatform.Api.Common.Responses;

/// <summary>
/// MRR (Monthly Recurring Revenue) analytics for dashboard
/// </summary>
public sealed class MrrAnalyticsDashboardResponse
{
    /// <summary>
    /// Total MRR from all active paid subscriptions
    /// </summary>
    public decimal TotalMrr { get; init; }

    /// <summary>
    /// Total MRR from monthly subscriptions only
    /// </summary>
    public decimal MonthlySalesMrr { get; init; }

    /// <summary>
    /// Total MRR from annual subscriptions (amortized monthly)
    /// </summary>
    public decimal AnnualSalesMrr { get; init; }

    /// <summary>
    /// Number of active paid subscriptions
    /// </summary>
    public long ActiveSubscriptions { get; init; }

    /// <summary>
    /// Number of trial subscriptions
    /// </summary>
    public long TrialSubscriptions { get; init; }

    /// <summary>
    /// Number of cancelled subscriptions
    /// </summary>
    public long CancelledSubscriptions { get; init; }

    /// <summary>
    /// Breakdown of MRR by subscription plan
    /// </summary>
    public List<MrrByPlanBreakdown> MrrByPlan { get; init; } = [];

    /// <summary>
    /// Date from which MRR is calculated
    /// </summary>
    public DateTime FromDate { get; init; }

    /// <summary>
    /// Date until which MRR is calculated
    /// </summary>
    public DateTime ToDate { get; init; }

    /// <summary>
    /// Percentage change in MRR compared to previous period (if available)
    /// </summary>
    public decimal? MrrChangePercentage { get; init; }
}

/// <summary>
/// MRR breakdown by subscription plan
/// </summary>
public sealed class MrrByPlanBreakdown
{
    /// <summary>
    /// Subscription plan name
    /// </summary>
    public string PlanName { get; init; } = string.Empty;

    /// <summary>
    /// MRR for this plan
    /// </summary>
    public decimal Mrr { get; init; }

    /// <summary>
    /// Number of active subscriptions for this plan
    /// </summary>
    public long SubscriptionCount { get; init; }

    /// <summary>
    /// Percentage of total MRR this plan represents
    /// </summary>
    public decimal PercentageOfTotalMrr { get; init; }
}
