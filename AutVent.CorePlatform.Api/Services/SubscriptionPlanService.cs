using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class SubscriptionPlanService(IUnitOfWork unitOfWork) : ISubscriptionPlanService
{
    public async Task<ApiResponse<PagedResponse<SubscriptionPlanResponse>>> GetAllAsync(PagedQueryRequest request, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = unitOfWork.Query<SubscriptionPlanDefinition>()
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(search) ||
                x.Plan.ToString().ToLower().Contains(search) ||
                (x.Description != null && x.Description.ToLower().Contains(search)));
        }

        if (request.Filters is not null &&
            request.Filters.TryGetValue("isActive", out var isActiveFilter) &&
            bool.TryParse(isActiveFilter, out var isActive))
        {
            query = query.Where(x => x.IsActive == isActive);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy?.ToLower() switch
        {
            "oldest" => query.OrderBy(x => x.Id),
            _ => query.OrderByDescending(x => x.Id)
        };

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SubscriptionPlanResponse
            {
                Id = x.Id,
                Plan = x.Plan.ToString(),
                Name = x.Name,
                Description = x.Description,
                MonthlyPrice = x.MonthlyPrice,
                AnnualPrice = x.AnnualPrice,
                MaxStores = x.MaxStores,
                MaxStaff = x.MaxStaff,
                MaxProducts = x.MaxProducts,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

        var paged = new PagedResponse<SubscriptionPlanResponse>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items
        };

        return ApiResponse<PagedResponse<SubscriptionPlanResponse>>.Ok(paged);
    }

    public async Task<ApiResponse<SubscriptionPlanResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var plan = await unitOfWork.Query<SubscriptionPlanDefinition>()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new SubscriptionPlanResponse
            {
                Id = x.Id,
                Plan = x.Plan.ToString(),
                Name = x.Name,
                Description = x.Description,
                MonthlyPrice = x.MonthlyPrice,
                AnnualPrice = x.AnnualPrice,
                MaxStores = x.MaxStores,
                MaxStaff = x.MaxStaff,
                MaxProducts = x.MaxProducts,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (plan is null)
        {
            return ApiResponse<SubscriptionPlanResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Subscription plan not found",
                [new ApiError("SubscriptionPlanNotFound", "No subscription plan found for this id", nameof(id))]);
        }

        return ApiResponse<SubscriptionPlanResponse>.Ok(plan);
    }
}
