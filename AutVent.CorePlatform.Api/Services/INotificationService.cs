using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface INotificationService
{
    /// <summary>Create and persist a single notification. Used internally by other services.</summary>
    Task CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Paged feed for the authenticated user, optionally filtered by store and read state.</summary>
    Task<ApiResponse<PagedResponse<NotificationResponse>>> GetFeedAsync(long userId, long? storeId, NotificationFeedRequest request, CancellationToken cancellationToken = default);

    /// <summary>Fast unread count for the navbar bell badge, optionally filtered by store.</summary>
    Task<ApiResponse<UnreadCountResponse>> GetUnreadCountAsync(long userId, long? storeId, CancellationToken cancellationToken = default);

    /// <summary>Mark a single notification as read.</summary>
    Task<ApiResponse<bool>> MarkReadAsync(long userId, long? storeId, long notificationId, CancellationToken cancellationToken = default);

    /// <summary>Mark every unread notification for the user as read, optionally scoped by store.</summary>
    Task<ApiResponse<bool>> MarkAllReadAsync(long userId, long? storeId, CancellationToken cancellationToken = default);

    /// <summary>Soft-delete / dismiss a single notification.</summary>
    Task<ApiResponse<bool>> DeleteAsync(long userId, long? storeId, long notificationId, CancellationToken cancellationToken = default);
}
