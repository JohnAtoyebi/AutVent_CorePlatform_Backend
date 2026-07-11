namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class MetricsResponse
{
    public MetricPeriod CurrentPeriod { get; init; } = null!;
    public MetricPeriod PreviousPeriod { get; init; } = null!;

    public MetricItem ProductsSold { get; init; } = null!;
    public MetricItem NewCustomers { get; init; } = null!;
    public MetricItem LowStock { get; init; } = null!;
    public MetricItem OutOfStock { get; init; } = null!;
}

public sealed class MetricPeriod
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
}

public sealed class MetricItem
{
    /// <summary>Value for the current period.</summary>
    public long Current { get; init; }

    /// <summary>Value for the previous equivalent period.</summary>
    public long Previous { get; init; }

    /// <summary>
    /// Percentage change from previous to current period.
    /// Positive = increase, Negative = decrease, null = no previous data.
    /// </summary>
    public decimal? PercentageChange { get; init; }

    /// <summary>Absolute difference (current - previous).</summary>
    public long Change { get; init; }

    public string Trend => Change > 0 ? "up" : Change < 0 ? "down" : "flat";
}

public sealed class SalesSummaryResponse
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }

    /// <summary>Sum of TotalAmount for all sales in the period.</summary>
    public decimal TotalSales { get; init; }

    /// <summary>Sum of AmountPaid for all sales in the period.</summary>
    public decimal TotalSettled { get; init; }

    /// <summary>Sum of BalanceRemaining for all sales in the period.</summary>
    public decimal TotalOwed { get; init; }

    /// <summary>Number of sales transactions in the period.</summary>
    public int TotalTransactions { get; init; }

    /// <summary>Breakdown by month within the period.</summary>
    public List<SalesSummaryMonthBreakdown> MonthlyBreakdown { get; init; } = [];
}

public sealed class SalesSummaryMonthBreakdown
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthName { get; init; } = string.Empty;
    public decimal TotalSales { get; init; }
    public decimal TotalSettled { get; init; }
    public decimal TotalOwed { get; init; }
    public int TotalTransactions { get; init; }
}

public sealed class SalesGraphResponse
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }

    /// <summary>"daily" or "monthly" depending on range length.</summary>
    public string Granularity { get; init; } = string.Empty;

    public List<SalesGraphPoint> Points { get; init; } = [];
}

public sealed class SalesGraphPoint
{
    /// <summary>ISO date label: "2025-07-11" (daily) or "2025-07" (monthly).</summary>
    public string Label { get; init; } = string.Empty;

    public decimal TotalSales { get; init; }
    public decimal TotalSettled { get; init; }
}
