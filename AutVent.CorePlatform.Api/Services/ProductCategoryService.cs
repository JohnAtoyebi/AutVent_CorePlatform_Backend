using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class ProductCategoryService(IUnitOfWork unitOfWork) : IProductCategoryService
{
    private const string SystemActor = "system";

    // ── Resolve business (shared helper) ──────────────────────────────────────
    private async Task<(Business? business, ApiResponse<T>? error)> ResolveBusiness<T>(long userId, CancellationToken ct)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);

        if (business is null)
        {
            var err = ApiResponse<T>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found for this user",
                [new ApiError("BusinessNotFound", "Create a business before managing categories", "userId")]);
            return (null, err);
        }

        return (business, null);
    }

    // ── GetAll — scoped to the business's mapped categories ───────────────────
    public async Task<ApiResponse<PagedResponse<CategoryResponse>>> GetAllAsync(
        PagedQueryRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var (business, err) = await ResolveBusiness<PagedResponse<CategoryResponse>>(userId, cancellationToken);
        if (err is not null) return err;

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        // Check whether this business has any mapped categories yet
        var hasMappings = await unitOfWork.Query<BusinessProductCategory>()
            .AnyAsync(m => m.BusinessId == business!.Id && !m.IsDeleted, cancellationToken);

        IQueryable<ProductCategory> query;

        if (hasMappings)
        {
            // Normal path: return what this business has mapped
            query = unitOfWork.Query<BusinessProductCategory>()
                .Where(m => m.BusinessId == business!.Id && !m.IsDeleted)
                .Include(m => m.ProductCategory)
                .Where(m => !m.ProductCategory.IsDeleted)
                .Select(m => m.ProductCategory);
        }
        else
        {
            // Onboarding / no mappings yet: fall back to system defaults
            query = unitOfWork.Query<ProductCategory>()
                .Where(x => x.IsDefault && !x.IsDeleted);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(search));
        }

        if (request.Filters is not null &&
            request.Filters.TryGetValue("isActive", out var isActiveFilter) &&
            bool.TryParse(isActiveFilter, out var isActive))
        {
            query = query.Where(x => x.IsActive == isActive);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CategoryResponse
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                IsDefault = x.IsDefault
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResponse<CategoryResponse>>.Ok(CreatePagedResponse(pageNumber, pageSize, totalCount, items));
    }

    // ── CreateBatch — find-or-create globally, then map to business ───────────
    public async Task<ApiResponse<IList<CategoryResponse>>> CreateBatchAsync(
        CreateCategoriesRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var (business, err) = await ResolveBusiness<IList<CategoryResponse>>(userId, cancellationToken);
        if (err is not null) return err;

        var now = DateTime.UtcNow;
        var createdItems = new List<CategoryResponse>();

        foreach (var item in request.Categories)
        {
            var name = item.Name.Trim();

            // Find or create the global catalog row
            var category = await unitOfWork.Query<ProductCategory>()
                .FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower(), cancellationToken);

            if (category is null)
            {
                category = new ProductCategory
                {
                    Name = name,
                    IsDefault = false,
                    CreatedByBusinessId = business!.Id,
                    IsActive = true,
                    CreatedBy = SystemActor,
                    DateCreated = now
                };
                await unitOfWork.CreateAsync(category, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken); // flush to get Id
            }

            // Upsert the business ↔ category mapping
            var mappingExists = await unitOfWork.Query<BusinessProductCategory>()
                .AnyAsync(m => m.BusinessId == business!.Id && m.ProductCategoryId == category.Id, cancellationToken);

            if (!mappingExists)
            {
                var mapping = new BusinessProductCategory
                {
                    BusinessId = business!.Id,
                    ProductCategoryId = category.Id,
                    IsActive = true,
                    CreatedBy = SystemActor,
                    DateCreated = now
                };
                await unitOfWork.CreateAsync(mapping, cancellationToken);
            }

            createdItems.Add(Map(category));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApiResponse<IList<CategoryResponse>>.Created(createdItems, "Product categories created successfully");
    }

    // ── Delete — remove business mapping only; never delete defaults ──────────
    public async Task<ApiResponse<bool>> DeleteAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var (business, err) = await ResolveBusiness<bool>(userId, cancellationToken);
        if (err is not null) return err;

        var mapping = await unitOfWork.Query<BusinessProductCategory>()
            .Include(m => m.ProductCategory)
            .FirstOrDefaultAsync(m => m.BusinessId == business!.Id && m.ProductCategoryId == id, cancellationToken);

        if (mapping is null)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status404NotFound,
                "Product category not found",
                [new ApiError("NotFound", "Product category not found for this business", nameof(id))]);
        }

        if (mapping.ProductCategory.IsDefault)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status400BadRequest,
                "Default categories cannot be removed",
                [new ApiError("CannotRemoveDefault", "System default categories cannot be removed from your business", nameof(id))]);
        }

        unitOfWork.Delete(mapping);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Product category removed successfully");
    }

    // ── DeleteBatch — remove multiple business mappings ───────────────────────
    public async Task<ApiResponse<IList<long>>> DeleteBatchAsync(IList<long> ids, long userId, CancellationToken cancellationToken = default)
    {
        if (!ids.Any())
        {
            return ApiResponse<IList<long>>.Failed(
                StatusCodes.Status400BadRequest,
                "No IDs provided for deletion",
                [new ApiError("EmptyIds", "At least one ID is required", nameof(ids))]);
        }

        var (business, err) = await ResolveBusiness<IList<long>>(userId, cancellationToken);
        if (err is not null) return err;

        var mappings = await unitOfWork.Query<BusinessProductCategory>()
            .Include(m => m.ProductCategory)
            .Where(m => m.BusinessId == business!.Id && ids.Contains(m.ProductCategoryId))
            .ToListAsync(cancellationToken);

        var foundIds = mappings.Select(m => m.ProductCategoryId).ToList();
        var notFoundIds = ids.Except(foundIds).ToList();
        var deletedIds = new List<long>();
        var errors = new List<ApiError>();

        foreach (var mapping in mappings)
        {
            if (mapping.ProductCategory.IsDefault)
            {
                errors.Add(new ApiError("CannotRemoveDefault",
                    $"'{mapping.ProductCategory.Name}' is a default category and cannot be removed",
                    nameof(ids)));
                continue;
            }

            unitOfWork.Delete(mapping);
            deletedIds.Add(mapping.ProductCategoryId);
        }

        foreach (var notFoundId in notFoundIds)
            errors.Add(new ApiError("NotFound", $"Product category with ID {notFoundId} not found", nameof(ids)));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (errors.Any())
        {
            var statusCode = deletedIds.Any() ? StatusCodes.Status207MultiStatus : StatusCodes.Status400BadRequest;
            return new ApiResponse<IList<long>>
            {
                Success = deletedIds.Any(),
                StatusCode = statusCode,
                Message = deletedIds.Any()
                    ? "Some product categories were removed, but others could not be"
                    : "No product categories were removed",
                Data = deletedIds,
                Errors = errors
            };
        }

        return ApiResponse<IList<long>>.Ok(deletedIds, "Product categories removed successfully");
    }

    // ── Internal helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Maps all default categories to a business. Called once when a business is first created.
    /// </summary>
    public static async Task MapDefaultsToBusinessAsync(IUnitOfWork uow, long businessId, CancellationToken cancellationToken = default)
    {
        var defaults = await uow.Query<ProductCategory>()
            .Where(c => c.IsDefault && !c.IsDeleted)
            .ToListAsync(cancellationToken);

        var existingMappingIds = await uow.Query<BusinessProductCategory>()
            .Where(m => m.BusinessId == businessId)
            .Select(m => m.ProductCategoryId)
            .ToListAsync(cancellationToken);

        foreach (var category in defaults)
        {
            if (existingMappingIds.Contains(category.Id)) continue;

            await uow.CreateAsync(new BusinessProductCategory
            {
                BusinessId = businessId,
                ProductCategoryId = category.Id,
                IsActive = true,
                CreatedBy = SystemActor,
                DateCreated = DateTime.UtcNow
            }, cancellationToken);
        }
    }

    private static CategoryResponse Map(ProductCategory entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            IsActive = entity.IsActive,
            IsDefault = entity.IsDefault
        };

    private static PagedResponse<T> CreatePagedResponse<T>(int pageNumber, int pageSize, int totalCount, IReadOnlyList<T> items) =>
        new()
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
}
