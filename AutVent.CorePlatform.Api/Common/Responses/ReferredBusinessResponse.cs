namespace AutVent.CorePlatform.Api.Common.Responses;

/// <summary>
/// Response representing a business that was referred by a user
/// </summary>
public sealed class ReferredBusinessResponse
{
    /// <summary>
    /// Database ID of the business
    /// </summary>
    public long BusinessId { get; init; }

    /// <summary>
    /// Name of the business
    /// </summary>
    public string BusinessName { get; init; } = string.Empty;

    /// <summary>
    /// Email address of the business
    /// </summary>
    public string? BusinessEmail { get; init; }

    /// <summary>
    /// Phone number of the business
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Website of the business
    /// </summary>
    public string? Website { get; init; }

    /// <summary>
    /// Country where business operates
    /// </summary>
    public string? Country { get; init; }

    /// <summary>
    /// City where business operates
    /// </summary>
    public string? City { get; init; }

    /// <summary>
    /// State where business operates
    /// </summary>
    public string? State { get; init; }

    /// <summary>
    /// Whether the business is currently active
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Date when the business was created
    /// </summary>
    public DateTime CreatedDate { get; init; }

    /// <summary>
    /// Name of the owner of this business
    /// </summary>
    public string OwnerName { get; init; } = string.Empty;

    /// <summary>
    /// Email of the business owner
    /// </summary>
    public string OwnerEmail { get; init; } = string.Empty;

    /// <summary>
    /// Current subscription status (e.g., Active, Trial, Cancelled)
    /// </summary>
    public string? SubscriptionStatus { get; init; }

    /// <summary>
    /// Name of the current subscription plan
    /// </summary>
    public string? SubscriptionPlanName { get; init; }

    /// <summary>
    /// Monthly price of the subscription
    /// </summary>
    public decimal? MonthlyPrice { get; init; }

    /// <summary>
    /// Annual price of the subscription
    /// </summary>
    public decimal? AnnualPrice { get; init; }

    /// <summary>
    /// Date when the subscription plan starts
    /// </summary>
    public DateTime? PlanStartDate { get; init; }

    /// <summary>
    /// Date when the subscription plan ends
    /// </summary>
    public DateTime? PlanEndDate { get; init; }

    /// <summary>
    /// Number of stores the business owns
    /// </summary>
    public long StoreCount { get; init; }

    /// <summary>
    /// Number of products the business has
    /// </summary>
    public long ProductCount { get; init; }
}
