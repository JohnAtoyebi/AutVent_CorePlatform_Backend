using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class SupplierService(IUnitOfWork unitOfWork) : ISupplierService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<SupplierResponse>> CreateAsync(CreateSupplierRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var normalizedName = request.Name.Trim();

        var nameExists = await unitOfWork.Query<Supplier>()
            .AnyAsync(x => x.Name.ToLower() == normalizedName.ToLower() && !x.IsDeleted, cancellationToken);

        if (nameExists)
        {
            return ApiResponse<SupplierResponse>.Failed(
                StatusCodes.Status409Conflict,
                "A supplier with this name already exists",
                [new ApiError("DuplicateSupplier", "Supplier name already exists", nameof(request.Name))]);
        }

        var supplier = new Supplier
        {
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
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (supplier is null)
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
            .Where(x => !x.IsDeleted);

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
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (supplier is null)
        {
            return ApiResponse<SupplierResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Supplier not found",
                [new ApiError("SupplierNotFound", "No supplier found for this id", nameof(id))]);
        }

        var normalizedName = request.Name.Trim();

        var nameExists = await unitOfWork.Query<Supplier>()
            .AnyAsync(x => x.Id != id && x.Name.ToLower() == normalizedName.ToLower() && !x.IsDeleted, cancellationToken);

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

        return ApiResponse<SupplierResponse>.Ok(MapToResponse(supplier), "Supplier updated successfully");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var supplier = await unitOfWork.Query<Supplier>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (supplier is null)
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

        return ApiResponse<bool>.Ok(true, "Supplier deleted successfully");
    }

    private static SupplierResponse MapToResponse(Supplier supplier) => new()
    {
        Id = supplier.Id,
        Name = supplier.Name,
        ContactEmail = supplier.ContactEmail,
        ContactPhone = supplier.ContactPhone,
        CreatedAt = supplier.DateCreated
    };
}
