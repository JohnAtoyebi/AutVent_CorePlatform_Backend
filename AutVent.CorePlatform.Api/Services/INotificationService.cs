using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface INotificationService
{
    /// <summary>Create and persist a single notification. Used internally by other services.</summary>
    Task CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Paged feed for the authenticated user, optionally filtered by read state.</summary>
    Task<ApiResponse<PagedResponse<NotificationResponse>>> GetFeedAsync(long userId, NotificationFeedRequest request, CancellationToken cancellationToken = default);

    /// <summary>Fast unread count for the navbar bell badge.</summary>
    Task<ApiResponse<UnreadCountResponse>> GetUnreadCountAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>Mark a single notification as read.</summary>
    Task<ApiResponse<bool>> MarkReadAsync(long userId, long notificationId, CancellationToken cancellationToken = default);

    /// <summary>Mark every unread notification for the user as read.</summary>
    Task<ApiResponse<bool>> MarkAllReadAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>Soft-delete / dismiss a single notification.</summary>
    Task<ApiResponse<bool>> DeleteAsync(long userId, long notificationId, CancellationToken cancellationToken = default);
}
