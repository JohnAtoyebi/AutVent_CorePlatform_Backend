using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class SupplierService(
    IUnitOfWork unitOfWork,
    IAuditLogService auditLogService,
    INotificationService notificationService,
    IAccessContext accessContext) : ISupplierService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<SupplierResponse>> CreateAsync(CreateSupplierRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.Id == request.BusinessId, cancellationToken);

        if (business is null)
        {
            return ApiResponse<SupplierResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found",
                [new ApiError("BusinessNotFound", "No business found for this id", nameof(request.BusinessId))]);
        }

        if (!accessContext.IsPlatformAdmin && business.UserId != userId)
        {
            return ApiResponse<SupplierResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "Business does not belong to the current user",
                [new ApiError("UnauthorizedBusiness", "The business does not belong to the current user", nameof(request.BusinessId))]);
        }

        var normalizedName = request.Name.Trim();

        var nameExists = await unitOfWork.Query<Supplier>()
            .AnyAsync(x => x.BusinessId == request.BusinessId && x.Name.ToLower() == normalizedName.ToLower() && !x.IsDeleted, cancellationToken);

        if (nameExists)
        {
            return ApiResponse<SupplierResponse>.Failed(
                StatusCodes.Status409Conflict,
                "A supplier with this name already exists for this business",
                [new ApiError("DuplicateSupplier", "Supplier name already exists for this business", nameof(request.Name))]);
        }

        var supplier = new Supplier
        {
            BusinessId = request.BusinessId,
            Name = normalizedName,
            ContactEmail = request.ContactEmail?.Trim().ToLowerInvariant(),
            ContactPhone = request.ContactPhone?.Trim(),
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = DateTime.UtcNow
        };

        await unitOfWork.CreateAsync(supplier, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<SupplierResponse>.Created(MapToResponse(supplier), "Supplier created successfully");
    }

    public async Task<ApiResponse<SupplierResponse>> GetByIdAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var supplier = await unitOfWork.Query<Supplier>()
            .Include(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (supplier is null)
        {
            return ApiResponse<SupplierResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Supplier not found",
                [new ApiError("SupplierNotFound", "No supplier found for this id", nameof(id))]);
        }

        if (!accessContext.IsPlatformAdmin && (supplier.Business is null || supplier.Business.UserId != userId))
        {
            return ApiResponse<SupplierResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Supplier not found",
                [new ApiError("SupplierNotFound", "No supplier found for this id", nameof(id))]);
        }

        return ApiResponse<SupplierResponse>.Ok(MapToResponse(supplier));
    }

    public async Task<ApiResponse<PagedResponse<SupplierResponse>>> GetAllAsync(PagedQueryRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var query = unitOfWork.Query<Supplier>()
            .Include(x => x.Business)
            .Where(x => !x.IsDeleted);

        if (!accessContext.IsPlatformAdmin)
        {
            query = query.Where(x => x.Business != null && x.Business.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(search) ||
                (x.ContactEmail != null && x.ContactEmail.ToLower().Contains(search)) ||
                (x.ContactPhone != null && x.ContactPhone.Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var suppliers = await query
            .OrderByDescending(x => x.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var pagedResponse = new PagedResponse<SupplierResponse>
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
            Items = suppliers.Select(MapToResponse).ToList()
        };

        return ApiResponse<PagedResponse<SupplierResponse>>.Ok(pagedResponse);
    }

    public async Task<ApiResponse<SupplierResponse>> UpdateAsync(long id, UpdateSupplierRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var supplier = await unitOfWork.Query<Supplier>()
            .Include(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (supplier is null)
        {
            return ApiResponse<SupplierResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Supplier not found",
                [new ApiError("SupplierNotFound", "No supplier found for this id", nameof(id))]);
        }

        if (!accessContext.IsPlatformAdmin && (supplier.Business is null || supplier.Business.UserId != userId))
        {
            return ApiResponse<SupplierResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Supplier not found",
                [new ApiError("SupplierNotFound", "No supplier found for this id", nameof(id))]);
        }

        var normalizedName = request.Name.Trim();

        var nameExists = await unitOfWork.Query<Supplier>()
            .AnyAsync(x => x.Id != id && x.BusinessId == supplier.BusinessId && x.Name.ToLower() == normalizedName.ToLower() && !x.IsDeleted, cancellationToken);

        if (nameExists)
        {
            return ApiResponse<SupplierResponse>.Failed(
                StatusCodes.Status409Conflict,
                "A supplier with this name already exists",
                [new ApiError("DuplicateSupplier", "Supplier name already exists", nameof(request.Name))]);
        }

        supplier.Name = normalizedName;
        supplier.ContactEmail = request.ContactEmail?.Trim().ToLowerInvariant();
        supplier.ContactPhone = request.ContactPhone?.Trim();
        supplier.UpdatedBy = SystemActor;
        supplier.DateUpdated = DateTime.UtcNow;

        unitOfWork.Update(supplier);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            userId,
            AuditAction.StoreUpdated,
            nameof(Supplier),
            $"Supplier '{supplier.Name}' updated.",
            entityId: supplier.Id,
            cancellationToken: cancellationToken);

        await notificationService.CreateAsync(new CreateNotificationRequest
        {
            UserId = userId,
            Type = NotificationType.General,
            Title = "Supplier Updated",
            Message = $"{supplier.Name} was updated successfully.",
            ActionUrl = "/suppliers"
        }, cancellationToken);

        return ApiResponse<SupplierResponse>.Ok(MapToResponse(supplier), "Supplier updated successfully");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var supplier = await unitOfWork.Query<Supplier>()
            .Include(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (supplier is null)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status404NotFound,
                "Supplier not found",
                [new ApiError("SupplierNotFound", "No supplier found for this id", nameof(id))]);
        }

        if (!accessContext.IsPlatformAdmin && (supplier.Business is null || supplier.Business.UserId != userId))
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status404NotFound,
                "Supplier not found",
                [new ApiError("SupplierNotFound", "No supplier found for this id", nameof(id))]);
        }

        var now = DateTime.UtcNow;

        supplier.IsDeleted = true;
        supplier.IsActive = false;
        supplier.DateDeleted = now;
        supplier.UpdatedBy = SystemActor;
        supplier.DateUpdated = now;

        unitOfWork.Update(supplier);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            userId,
            AuditAction.SupplierDeleted,
            nameof(Supplier),
            $"Supplier '{supplier.Name}' deleted.",
            entityId: supplier.Id,
            cancellationToken: cancellationToken);

        await notificationService.CreateAsync(new CreateNotificationRequest
        {
            UserId = userId,
            Type = NotificationType.General,
            Title = "Supplier Deleted",
            Message = $"{supplier.Name} was deleted.",
            ActionUrl = "/suppliers"
        }, cancellationToken);

        return ApiResponse<bool>.Ok(true, "Supplier deleted successfully");
    }

    private static SupplierResponse MapToResponse(Supplier supplier) => new()
    {
        Id = supplier.Id,
        BusinessId = supplier.BusinessId,
        Name = supplier.Name,
        ContactEmail = supplier.ContactEmail,
        ContactPhone = supplier.ContactPhone,
        CreatedAt = supplier.DateCreated
    };
}
