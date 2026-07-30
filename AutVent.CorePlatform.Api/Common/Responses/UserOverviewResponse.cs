namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class UserOverviewResponse
{
    public long TotalUsers { get; init; }
    public long ActiveUsers { get; init; }
    public long SuspendedUsers { get; init; }
}
