namespace AutVent.CorePlatform.Api.Common.Responses;

/// <summary>
/// Dashboard response for user statistics
/// </summary>
public sealed class UserStatisticsDashboardResponse
{
    /// <summary>
    /// Total number of users on the platform
    /// </summary>
    public long TotalUsers { get; init; }

    /// <summary>
    /// Number of active users
    /// </summary>
    public long ActiveUsers { get; init; }

    /// <summary>
    /// Number of inactive/suspended users
    /// </summary>
    public long InactiveUsers { get; init; }

    /// <summary>
    /// Number of users who are platform admins
    /// </summary>
    public long AdminUsers { get; init; }

    /// <summary>
    /// Number of users who are business owners
    /// </summary>
    public long BusinessOwnerUsers { get; init; }

    /// <summary>
    /// Number of staff users across businesses
    /// </summary>
    public long StaffUsers { get; init; }

    /// <summary>
    /// Total number of new users in the specified date range
    /// </summary>
    public long NewUsersInPeriod { get; init; }

    /// <summary>
    /// Date from which statistics are calculated
    /// </summary>
    public DateTime FromDate { get; init; }

    /// <summary>
    /// Date until which statistics are calculated
    /// </summary>
    public DateTime ToDate { get; init; }
}
