using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class CategoryService(IUnitOfWork unitOfWork) : ICategoryService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<PagedResponse<CategoryResponse>>> GetBusinessIndustriesAsync(PagedQueryRequest request, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = unitOfWork.Query<BusinessIndustry>().AsQueryable();

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
            .OrderBy(x => x.Name)
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

    public async Task<ApiResponse<CategoryResponse>> CreateBusinessIndustryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        var exists = await unitOfWork.Query<BusinessIndustry>()
            .AnyAsync(x => x.Name.ToLower() == name.ToLower(), cancellationToken);

        if (exists)
        {
            return ApiResponse<CategoryResponse>.Failed(
                StatusCodes.Status409Conflict,
                "Business industry already exists",
                [new ApiError("DuplicateBusinessIndustry", "Business industry already exists", nameof(request.Name))]);
        }

        var entity = new BusinessIndustry
        {
            Name = name,
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = DateTime.UtcNow
        };

        await unitOfWork.CreateAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CategoryResponse>.Created(Map(entity), "Business industry created successfully");
    }

    public async Task<ApiResponse<PagedResponse<CategoryResponse>>> GetProductCategoriesAsync(PagedQueryRequest request, CancellationToken cancellationToken = default)
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
            .OrderBy(x => x.Name)
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

    public async Task<ApiResponse<CategoryResponse>> CreateProductCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        var exists = await unitOfWork.Query<ProductCategory>()
            .AnyAsync(x => x.Name.ToLower() == name.ToLower(), cancellationToken);

        if (exists)
        {
            return ApiResponse<CategoryResponse>.Failed(
                StatusCodes.Status409Conflict,
                "Product category already exists",
                [new ApiError("DuplicateProductCategory", "Product category already exists", nameof(request.Name))]);
        }

        var entity = new ProductCategory
        {
            Name = name,
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = DateTime.UtcNow
        };

        await unitOfWork.CreateAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CategoryResponse>.Created(Map(entity), "Product category created successfully");
    }

    public async Task<ApiResponse<PagedResponse<CategoryResponse>>> GetStoreCategoriesAsync(PagedQueryRequest request, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = unitOfWork.Query<StoreCategory>().AsQueryable();

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
            .OrderBy(x => x.Name)
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

    public async Task<ApiResponse<CategoryResponse>> CreateStoreCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        var exists = await unitOfWork.Query<StoreCategory>()
            .AnyAsync(x => x.Name.ToLower() == name.ToLower(), cancellationToken);

        if (exists)
        {
            return ApiResponse<CategoryResponse>.Failed(
                StatusCodes.Status409Conflict,
                "Store category already exists",
                [new ApiError("DuplicateStoreCategory", "Store category already exists", nameof(request.Name))]);
        }

        var entity = new StoreCategory
        {
            Name = name,
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = DateTime.UtcNow
        };

        await unitOfWork.CreateAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CategoryResponse>.Created(Map(entity), "Store category created successfully");
    }

    private static PagedResponse<CategoryResponse> CreatePagedResponse(int pageNumber, int pageSize, int totalCount, IReadOnlyCollection<CategoryResponse> items) => new()
    {
        PageNumber = pageNumber,
        PageSize = pageSize,
        TotalCount = totalCount,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        Items = items
    };

    private static CategoryResponse Map(BaseEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity switch
        {
            BusinessIndustry businessIndustry => businessIndustry.Name,
            ProductCategory productCategory => productCategory.Name,
            StoreCategory storeCategory => storeCategory.Name,
            _ => string.Empty
        },
        IsActive = entity.IsActive
    };
}
