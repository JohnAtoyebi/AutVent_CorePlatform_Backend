namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class MetricsRequest
{
    /// <summary>Start of the current period (UTC). Defaults to 30 days ago.</summary>
    public DateTime? From { get; init; }

    /// <summary>End of the current period (UTC). Defaults to now.</summary>
    public DateTime? To { get; init; }

    /// <summary>Filter to a specific store. If null, all stores in the business are included.</summary>
    public long? StoreId { get; init; }
}

public sealed class SalesSummaryRequest
{
    /// <summary>Filter by a specific year (e.g. 2025). Takes precedence over From/To if set.</summary>
    public int? Year { get; init; }

    /// <summary>Start of date range (UTC). Ignored when Year is provided.</summary>
    public DateTime? From { get; init; }

    /// <summary>End of date range (UTC). Ignored when Year is provided.</summary>
    public DateTime? To { get; init; }

    /// <summary>Filter to a specific store. If null, all stores in the business are included.</summary>
    public long? StoreId { get; init; }
}
