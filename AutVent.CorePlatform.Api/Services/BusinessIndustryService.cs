using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class BusinessIndustryService(IUnitOfWork unitOfWork) : IBusinessIndustryService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<PagedResponse<CategoryResponse>>> GetAllAsync(PagedQueryRequest request, CancellationToken cancellationToken = default)
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

    public async Task<ApiResponse<CategoryResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await unitOfWork.Query<BusinessIndustry>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return ApiResponse<CategoryResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Business industry not found",
                [new ApiError("NotFound", "Business industry not found", nameof(id))]);
        }

        return ApiResponse<CategoryResponse>.Ok(Map(entity));
    }

    public async Task<ApiResponse<IList<CategoryResponse>>> CreateBatchAsync(CreateCategoriesRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<ApiError>();
        var createdItems = new List<CategoryResponse>();

        foreach (var item in request.Categories)
        {
            var name = item.Name.Trim();
            var exists = await unitOfWork.Query<BusinessIndustry>()
                .AnyAsync(x => x.Name.ToLower() == name.ToLower(), cancellationToken);

            if (exists)
            {
                errors.Add(new ApiError("DuplicateBusinessIndustry", $"Business industry '{name}' already exists", nameof(item.Name)));
                continue;
            }

            var entity = new BusinessIndustry
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
                    ? "Some business industries were created successfully, but others failed" 
                    : "No business industries were created",
                Data = createdItems,
                Errors = errors
            };
        }

        return ApiResponse<IList<CategoryResponse>>.Created(createdItems, "Business industries created successfully");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await unitOfWork.Query<BusinessIndustry>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status404NotFound,
                "Business industry not found",
                [new ApiError("NotFound", "Business industry not found", nameof(id))]);
        }

        unitOfWork.Delete(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Business industry deleted successfully");
    }

    private static CategoryResponse Map(BusinessIndustry entity) =>
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
