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

    public async Task<ApiResponse<PagedResponse<NotificationResponse>>> GetFeedAsync(long userId, NotificationFeedRequest request, CancellationToken cancellationToken = default)
    {
        var query = unitOfWork.Query<Notification>()
            .Where(x => x.UserId == userId && !x.IsDeleted);

        if (request.IsRead.HasValue)
            query = query.Where(x => x.IsRead == request.IsRead.Value);

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

    public async Task<ApiResponse<UnreadCountResponse>> GetUnreadCountAsync(long userId, CancellationToken cancellationToken = default)
    {
        var count = await unitOfWork.Query<Notification>()
            .CountAsync(x => x.UserId == userId && !x.IsRead && !x.IsDeleted, cancellationToken);

        return ApiResponse<UnreadCountResponse>.Ok(new UnreadCountResponse { Count = count });
    }

    public async Task<ApiResponse<bool>> MarkReadAsync(long userId, long notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await unitOfWork.Query<Notification>()
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId && !x.IsDeleted, cancellationToken);

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

    public async Task<ApiResponse<bool>> MarkAllReadAsync(long userId, CancellationToken cancellationToken = default)
    {
        var unread = await unitOfWork.Query<Notification>()
            .Where(x => x.UserId == userId && !x.IsRead && !x.IsDeleted)
            .ToListAsync(cancellationToken);

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

    public async Task<ApiResponse<bool>> DeleteAsync(long userId, long notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await unitOfWork.Query<Notification>()
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId && !x.IsDeleted, cancellationToken);

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
        Type = n.Type,
        Title = n.Title,
        Message = n.Message,
        ActionUrl = n.ActionUrl,
        IsRead = n.IsRead,
        ReadAt = n.ReadAt,
        CreatedAt = n.DateCreated
    };
}
