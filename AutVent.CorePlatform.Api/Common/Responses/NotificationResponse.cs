using AutVent.CorePlatform.Domain.Enums;

namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class NotificationResponse
{
    public long Id { get; init; }
    public long? StoreId { get; init; }
    public NotificationType Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? ActionUrl { get; init; }
    public bool IsRead { get; init; }
    public DateTime? ReadAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class UnreadCountResponse
{
    public int Count { get; init; }
}
