using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class AdminUserService(
    IUnitOfWork unitOfWork,
    IAuditLogService auditLogService,
    INotificationService notificationService,
    IAccessContext accessContext) : IAdminUserService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<UserOverviewResponse>> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        if (!accessContext.IsPlatformAdmin)
        {
            return ApiResponse<UserOverviewResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to user overview",
                [new ApiError("Forbidden", "Only platform admins can view user overview")]);
        }

        var totalUsers = await unitOfWork.Query<User>()
            .CountAsync(x => !x.IsDeleted, cancellationToken);

        var activeUsers = await unitOfWork.Query<User>()
            .CountAsync(x => !x.IsDeleted && x.IsActive, cancellationToken);

        var suspendedUsers = await unitOfWork.Query<User>()
            .CountAsync(x => !x.IsDeleted && !x.IsActive, cancellationToken);

        return ApiResponse<UserOverviewResponse>.Ok(new UserOverviewResponse
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            SuspendedUsers = suspendedUsers
        });
    }

    public async Task<ApiResponse<PagedResponse<UserProfileResponse>>> GetAllAsync(PagedQueryRequest request, CancellationToken cancellationToken = default)
    {
        if (!accessContext.IsPlatformAdmin)
        {
            return ApiResponse<PagedResponse<UserProfileResponse>>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to users",
                [new ApiError("Forbidden", "Only platform admins can view users")]);
        }

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = unitOfWork.Query<User>()
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.FullName.ToLower().Contains(search) ||
                x.EmailAddress.ToLower().Contains(search) ||
                x.PhoneNumber.ToLower().Contains(search));
        }

        if (request.Filters is not null)
        {
            if (request.Filters.TryGetValue("isActive", out var isActiveFilter) && bool.TryParse(isActiveFilter, out var isActive))
            {
                query = query.Where(x => x.IsActive == isActive);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserProfileResponse
            {
                Id = x.Id,
                FullName = x.FullName,
                EmailAddress = x.EmailAddress,
                PhoneNumber = x.PhoneNumber,
                ReferralCode = x.ReferralCode,
                IsActive = x.IsActive,
                ProfilePhotoUrl = x.ProfilePhotoUrl,
                MemberSince = x.DateCreated
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResponse<UserProfileResponse>>.Ok(new PagedResponse<UserProfileResponse>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items
        });
    }

    public async Task<ApiResponse<bool>> ActivateAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        if (!accessContext.IsPlatformAdmin)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to activate users",
                [new ApiError("Forbidden", "Only platform admins can activate users", nameof(id))]);
        }

        var user = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (user is null)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status404NotFound,
                "User not found",
                [new ApiError("UserNotFound", "No user found for this id", nameof(id))]);
        }

        if (user.IsActive)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status400BadRequest,
                "User is already active",
                [new ApiError("AlreadyActive", "This user is already active", nameof(id))]);
        }

        user.IsActive = true;
        user.UpdatedBy = SystemActor;
        user.DateUpdated = DateTime.UtcNow;

        unitOfWork.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            userId,
            AuditAction.UserProfileUpdated,
            nameof(User),
            $"User '{user.EmailAddress}' activated.",
            entityId: user.Id,
            cancellationToken: cancellationToken);

        await notificationService.CreateAsync(new CreateNotificationRequest
        {
            UserId = user.Id,
            Type = NotificationType.General,
            Title = "User Activated",
            Message = "Your account has been activated.",
            ActionUrl = "/profile"
        }, cancellationToken);

        return ApiResponse<bool>.Ok(true, "User activated successfully");
    }

    public async Task<ApiResponse<bool>> DeactivateAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        if (!accessContext.IsPlatformAdmin)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to deactivate users",
                [new ApiError("Forbidden", "Only platform admins can deactivate users", nameof(id))]);
        }

        var user = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (user is null)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status404NotFound,
                "User not found",
                [new ApiError("UserNotFound", "No user found for this id", nameof(id))]);
        }

        if (!user.IsActive)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status400BadRequest,
                "User is already inactive",
                [new ApiError("AlreadyInactive", "This user is already inactive", nameof(id))]);
        }

        user.IsActive = false;
        user.UpdatedBy = SystemActor;
        user.DateUpdated = DateTime.UtcNow;

        unitOfWork.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            userId,
            AuditAction.UserProfileUpdated,
            nameof(User),
            $"User '{user.EmailAddress}' deactivated.",
            entityId: user.Id,
            cancellationToken: cancellationToken);

        await notificationService.CreateAsync(new CreateNotificationRequest
        {
            UserId = user.Id,
            Type = NotificationType.General,
            Title = "User Deactivated",
            Message = "Your account has been deactivated.",
            ActionUrl = "/profile"
        }, cancellationToken);

        return ApiResponse<bool>.Ok(true, "User deactivated successfully");
    }
}
