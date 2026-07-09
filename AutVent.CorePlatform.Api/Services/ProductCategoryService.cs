using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class ProductCategoryService(IUnitOfWork unitOfWork) : IProductCategoryService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<PagedResponse<CategoryResponse>>> GetAllAsync(PagedQueryRequest request, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = unitOfWork.Query<ProductCategory>().AsQueryable();

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
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResponse<CategoryResponse>>.Ok(CreatePagedResponse(pageNumber, pageSize, totalCount, items));
    }

    public async Task<ApiResponse<IList<CategoryResponse>>> CreateBatchAsync(CreateCategoriesRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<ApiError>();
        var createdItems = new List<CategoryResponse>();

        foreach (var item in request.Categories)
        {
            var name = item.Name.Trim();
            var exists = await unitOfWork.Query<ProductCategory>()
                .AnyAsync(x => x.Name.ToLower() == name.ToLower(), cancellationToken);

            if (exists)
            {
                errors.Add(new ApiError("DuplicateProductCategory", $"Product category '{name}' already exists", nameof(item.Name)));
                continue;
            }

            var entity = new ProductCategory
            {
                Name = name,
                IsActive = true,
                CreatedBy = SystemActor,
                DateCreated = DateTime.UtcNow
            };

            await unitOfWork.CreateAsync(entity, cancellationToken);
            createdItems.Add(Map(entity));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (errors.Any())
        {
            var statusCode = createdItems.Any() ? StatusCodes.Status207MultiStatus : StatusCodes.Status400BadRequest;
            return new ApiResponse<IList<CategoryResponse>>
            {
                Success = createdItems.Any(),
                StatusCode = statusCode,
                Message = createdItems.Any() 
                    ? "Some product categories were created successfully, but others failed" 
                    : "No product categories were created",
                Data = createdItems,
                Errors = errors
            };
        }

        return ApiResponse<IList<CategoryResponse>>.Created(createdItems, "Product categories created successfully");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await unitOfWork.Query<ProductCategory>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status404NotFound,
                "Product category not found",
                [new ApiError("NotFound", "Product category not found", nameof(id))]);
        }

        unitOfWork.Delete(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Product category deleted successfully");
    }

    public async Task<ApiResponse<IList<long>>> DeleteBatchAsync(IList<long> ids, CancellationToken cancellationToken = default)
    {
        if (!ids.Any())
        {
            return ApiResponse<IList<long>>.Failed(
                StatusCodes.Status400BadRequest,
                "No IDs provided for deletion",
                [new ApiError("EmptyIds", "At least one ID is required", nameof(ids))]);
        }

        var entities = await unitOfWork.Query<ProductCategory>()
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var notFoundIds = ids.Except(entities.Select(x => x.Id)).ToList();
        var deletedIds = new List<long>();

        foreach (var entity in entities)
        {
            unitOfWork.Delete(entity);
            deletedIds.Add(entity.Id);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (notFoundIds.Any())
        {
            var errors = notFoundIds.Select(id => new ApiError("NotFound", $"Product category with ID {id} not found", nameof(ids))).ToList();
            var statusCode = deletedIds.Any() ? StatusCodes.Status207MultiStatus : StatusCodes.Status404NotFound;
            
            return new ApiResponse<IList<long>>
            {
                Success = deletedIds.Any(),
                StatusCode = statusCode,
                Message = deletedIds.Any() 
                    ? "Some product categories were deleted, but others were not found" 
                    : "No product categories were deleted",
                Data = deletedIds,
                Errors = errors
            };
        }

        return ApiResponse<IList<long>>.Ok(deletedIds, "Product categories deleted successfully");
    }

    private static CategoryResponse Map(ProductCategory entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            IsActive = entity.IsActive
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
