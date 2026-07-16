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

public sealed class ProductMetricsResponse
{
    public MetricPeriod CurrentPeriod { get; init; } = null!;
    public MetricPeriod PreviousPeriod { get; init; } = null!;

    /// <summary>Total active products with % change vs previous period.</summary>
    public MetricItem TotalProducts { get; init; } = null!;

    /// <summary>Total distinct active categories that have at least one product.</summary>
    public int TotalCategories { get; init; }

    /// <summary>Average price across all active products in scope.</summary>
    public decimal AveragePrice { get; init; }

    /// <summary>Top products by units sold in the current period.</summary>
    public List<MostSoldProductResponse> MostSold { get; init; } = [];
}

public sealed class MostSoldProductResponse
{
    public long ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public long UnitsSold { get; init; }
    public decimal Revenue { get; init; }
}

public sealed class InventoryMetricsResponse
{
    public MetricPeriod CurrentPeriod { get; init; } = null!;
    public MetricPeriod PreviousPeriod { get; init; } = null!;

    /// <summary>Sum of (Quantity * CostPrice) for all active products in scope, with % change.</summary>
    public MetricItemDecimal TotalStockValue { get; init; } = null!;

    /// <summary>Products where Quantity > 0 and Quantity <= ReorderThreshold.</summary>
    public MetricItem LowStockItems { get; init; } = null!;

    /// <summary>Products where Quantity == 0.</summary>
    public MetricItem OutOfStockItems { get; init; } = null!;

    /// <summary>Stores in scope with their stock snapshot.</summary>
    public List<StockLocationResponse> StockLocations { get; init; } = [];
}

/// <summary>MetricItem variant for decimal values (e.g. monetary amounts).</summary>
public sealed class MetricItemDecimal
{
    public decimal Current { get; init; }
    public decimal Previous { get; init; }
    public decimal Change { get; init; }
    public decimal? PercentageChange { get; init; }
    public string Trend => Change > 0 ? "up" : Change < 0 ? "down" : "flat";
}

public sealed class StockLocationResponse
{
    public long StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public int TotalProducts { get; init; }
    public long TotalUnits { get; init; }
    public decimal TotalStockValue { get; init; }
    public int LowStockCount { get; init; }
    public int OutOfStockCount { get; init; }
}

public sealed class CustomerMetricsResponse
{
    public MetricPeriod CurrentPeriod { get; init; } = null!;
    public MetricPeriod PreviousPeriod { get; init; } = null!;

    /// <summary>All-time total customers in scope, with % change vs previous period.</summary>
    public MetricItem TotalCustomers { get; init; } = null!;

    /// <summary>Customers first created within the current period, with % change.</summary>
    public MetricItem NewCustomers { get; init; } = null!;

    /// <summary>Sum of BalanceRemaining across all unpaid/part-paid sales in scope.</summary>
    public decimal OutstandingBalance { get; init; }

    /// <summary>
    /// Percentage of customers in scope who made more than one purchase in the current period.
    /// </summary>
    public decimal RepeatCustomerPercentage { get; init; }

    /// <summary>Number of customers who made more than one purchase in the current period.</summary>
    public int RepeatCustomerCount { get; init; }
}
