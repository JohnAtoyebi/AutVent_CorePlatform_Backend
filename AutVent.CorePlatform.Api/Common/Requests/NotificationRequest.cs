using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class NotificationFeedRequest
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    /// <summary>When set, filters to only read (true) or unread (false) notifications.</summary>
    public bool? IsRead { get; init; }
}

public sealed class CreateNotificationRequest
{
    public long UserId { get; init; }
    public long? BusinessId { get; init; }
    public Domain.Enums.NotificationType Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? ActionUrl { get; init; }
}
