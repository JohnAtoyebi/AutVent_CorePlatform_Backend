using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class NotificationService(IUnitOfWork unitOfWork) : INotificationService
{
    private const string SystemActor = "system";

    public async Task CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = request.UserId,
            StoreId = request.StoreId,
            BusinessId = request.BusinessId,
            Type = request.Type,
            Channel = NotificationChannel.InApp,
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            ActionUrl = request.ActionUrl?.Trim(),
            IsRead = false,
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = DateTime.UtcNow
        };

        await unitOfWork.CreateAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ApiResponse<PagedResponse<NotificationResponse>>> GetFeedAsync(long userId, long? storeId, NotificationFeedRequest request, CancellationToken cancellationToken = default)
    {
        var query = unitOfWork.Query<Notification>()
            .Where(x => x.UserId == userId && !x.IsDeleted);

        // Filter by store if provided
        if (storeId.HasValue && storeId.Value > 0)
            query = query.Where(x => x.StoreId == storeId.Value);

        if (request.IsRead.HasValue)
            query = query.Where(x => x.IsRead == request.IsRead.Value);

        if (request.StoreId.HasValue && request.StoreId.Value > 0)
            query = query.Where(x => x.StoreId == request.StoreId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var notifications = await query
            .OrderByDescending(x => x.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var paged = new PagedResponse<NotificationResponse>
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
            Items = notifications.Select(MapToResponse).ToList()
        };

        return ApiResponse<PagedResponse<NotificationResponse>>.Ok(paged);
    }

    public async Task<ApiResponse<UnreadCountResponse>> GetUnreadCountAsync(long userId, long? storeId, CancellationToken cancellationToken = default)
    {
        var query = unitOfWork.Query<Notification>()
            .Where(x => x.UserId == userId && !x.IsRead && !x.IsDeleted);

        // Filter by store if provided
        if (storeId.HasValue && storeId.Value > 0)
            query = query.Where(x => x.StoreId == storeId.Value);

        var count = await query.CountAsync(cancellationToken);

        return ApiResponse<UnreadCountResponse>.Ok(new UnreadCountResponse { Count = count });
    }

    public async Task<ApiResponse<bool>> MarkReadAsync(long userId, long? storeId, long notificationId, CancellationToken cancellationToken = default)
    {
        var query = unitOfWork.Query<Notification>()
            .Where(x => x.Id == notificationId && x.UserId == userId && !x.IsDeleted);

        // Filter by store if provided
        if (storeId.HasValue && storeId.Value > 0)
            query = query.Where(x => x.StoreId == storeId.Value);

        var notification = await query.FirstOrDefaultAsync(cancellationToken);

        if (notification is null)
            return ApiResponse<bool>.Failed(StatusCodes.Status404NotFound,
                "Notification not found",
                [new ApiError("NotificationNotFound", "No notification found for this id", nameof(notificationId))]);

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            notification.DateUpdated = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse<bool>.Ok(true, "Notification marked as read");
    }

    public async Task<ApiResponse<bool>> MarkAllReadAsync(long userId, long? storeId, CancellationToken cancellationToken = default)
    {
        var query = unitOfWork.Query<Notification>()
            .Where(x => x.UserId == userId && !x.IsRead && !x.IsDeleted);

        // Filter by store if provided
        if (storeId.HasValue && storeId.Value > 0)
            query = query.Where(x => x.StoreId == storeId.Value);

        var unread = await query.ToListAsync(cancellationToken);

        if (unread.Count == 0)
            return ApiResponse<bool>.Ok(true, "No unread notifications");

        var now = DateTime.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = now;
            n.DateUpdated = now;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, $"{unread.Count} notification(s) marked as read");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(long userId, long? storeId, long notificationId, CancellationToken cancellationToken = default)
    {
        var query = unitOfWork.Query<Notification>()
            .Where(x => x.Id == notificationId && x.UserId == userId && !x.IsDeleted);

        // Filter by store if provided
        if (storeId.HasValue && storeId.Value > 0)
            query = query.Where(x => x.StoreId == storeId.Value);

        var notification = await query.FirstOrDefaultAsync(cancellationToken);

        if (notification is null)
            return ApiResponse<bool>.Failed(StatusCodes.Status404NotFound,
                "Notification not found",
                [new ApiError("NotificationNotFound", "No notification found for this id", nameof(notificationId))]);

        notification.IsDeleted = true;
        notification.DateUpdated = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Notification dismissed");
    }

    private static NotificationResponse MapToResponse(Notification n) => new()
    {
        Id = n.Id,
        StoreId = n.StoreId,
        Type = n.Type,
        Title = n.Title,
        Message = n.Message,
        ActionUrl = n.ActionUrl,
        IsRead = n.IsRead,
        ReadAt = n.ReadAt,
        CreatedAt = n.DateCreated
    };
}
