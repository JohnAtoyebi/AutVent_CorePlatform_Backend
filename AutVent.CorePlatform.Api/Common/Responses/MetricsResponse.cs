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

// ── Sales-by-location ───────────────────────────────────────────────────────

public sealed class SalesByLocationResponse
{
    public MetricPeriod CurrentPeriod { get; init; } = null!;
    public List<LocationSalesRow> Locations { get; init; } = [];
}

public sealed class LocationSalesRow
{
    public long StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public int TransactionCount { get; init; }
    public decimal Revenue { get; init; }
    public decimal RevenueShare { get; init; }
}

// ── Sales-by-category ───────────────────────────────────────────────────────

public sealed class SalesByCategoryResponse
{
    public MetricPeriod CurrentPeriod { get; init; } = null!;
    public List<CategorySalesRow> Categories { get; init; } = [];
}

public sealed class CategorySalesRow
{
    public long CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public long UnitsSold { get; init; }
    public decimal Revenue { get; init; }
    public decimal RevenueShare { get; init; }
}

// ── Payment-method breakdown ────────────────────────────────────────────────

public sealed class PaymentMethodBreakdownResponse
{
    public MetricPeriod CurrentPeriod { get; init; } = null!;
    public List<PaymentMethodRow> Methods { get; init; } = [];
}

public sealed class PaymentMethodRow
{
    public string Method { get; init; } = string.Empty;
    public int TransactionCount { get; init; }
    public decimal Revenue { get; init; }
    public decimal RevenueShare { get; init; }
}

// ── Top customers ───────────────────────────────────────────────────────────

public sealed class TopCustomersResponse
{
    public MetricPeriod CurrentPeriod { get; init; } = null!;
    public List<TopCustomerRow> BySpend { get; init; } = [];
    public List<TopCustomerRow> ByOrderCount { get; init; } = [];
}

public sealed class TopCustomerRow
{
    public long CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public int OrderCount { get; init; }
    public decimal TotalSpend { get; init; }
}

// ── Customer growth time-series ─────────────────────────────────────────────

public sealed class CustomerGrowthResponse
{
    public MetricPeriod CurrentPeriod { get; init; } = null!;
    public List<CustomerGrowthPoint> Series { get; init; } = [];
}

public sealed class CustomerGrowthPoint
{
    public string Label { get; init; } = string.Empty;
    public int NewCustomers { get; init; }
    public int CumulativeTotal { get; init; }
}

// ── Staff analytics ─────────────────────────────────────────────────────────

public sealed class StaffAnalyticsResponse
{
    public MetricPeriod CurrentPeriod { get; init; } = null!;
    public List<StaffAnalyticsRow> Staff { get; init; } = [];
}

public sealed class StaffAnalyticsRow
{
    public long StaffId { get; init; }
    public string StaffName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public int TransactionCount { get; init; }
    public decimal Revenue { get; init; }
    public decimal AverageOrderValue { get; init; }
}

// ── Extended product metrics ─────────────────────────────────────────────────

public sealed class ProductPerformanceResponse
{
    public MetricPeriod CurrentPeriod { get; init; } = null!;
    public MetricPeriod PreviousPeriod { get; init; } = null!;
    public List<MostSoldProductResponse> MostSold { get; init; } = [];
    public List<MostProfitableProductRow> MostProfitable { get; init; } = [];
    public List<FastestGrowingProductRow> FastestGrowing { get; init; } = [];
    public List<SlowMovingProductRow> SlowMoving { get; init; } = [];
}

public sealed class MostProfitableProductRow
{
    public long ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public long UnitsSold { get; init; }
    public decimal Revenue { get; init; }
    public decimal GrossProfit { get; init; }
    public decimal? ProfitMarginPct { get; init; }
}

public sealed class FastestGrowingProductRow
{
    public long ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public long CurrentUnitsSold { get; init; }
    public long PreviousUnitsSold { get; init; }
    public decimal? GrowthPct { get; init; }
}

public sealed class SlowMovingProductRow
{
    public long ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public long StockOnHand { get; init; }
    public decimal? CostPrice { get; init; }
    public decimal? StockValue { get; init; }
}

// ── Financial analytics ──────────────────────────────────────────────────────

public sealed class FinancialMetricsResponse
{
    public MetricPeriod CurrentPeriod { get; init; } = null!;
    public MetricPeriod PreviousPeriod { get; init; } = null!;
    public MetricItemDecimal Revenue { get; init; } = null!;
    public decimal CostOfGoods { get; init; }
    public decimal GrossProfit { get; init; }
    public decimal? GrossProfitMarginPct { get; init; }
    public List<CategoryProfitRow> ProfitByCategory { get; init; } = [];
}

public sealed class CategoryProfitRow
{
    public long CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public decimal Revenue { get; init; }
    public decimal CostOfGoods { get; init; }
    public decimal GrossProfit { get; init; }
    public decimal? GrossProfitMarginPct { get; init; }
}
