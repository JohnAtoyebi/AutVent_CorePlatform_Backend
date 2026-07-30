using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class AdminBusinessService(
    IUnitOfWork unitOfWork,
    IAuditLogService auditLogService,
    INotificationService notificationService,
    IAccessContext accessContext) : IAdminBusinessService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<BusinessOverviewResponse>> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        if (!accessContext.IsPlatformAdmin)
        {
            return ApiResponse<BusinessOverviewResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to business overview",
                [new ApiError("Forbidden", "Only platform admins can view business overview")]);
        }

        var totalBusiness = await unitOfWork.Query<Business>()
            .CountAsync(x => !x.IsDeleted, cancellationToken);

        var activeBusiness = await unitOfWork.Query<Business>()
            .CountAsync(x => !x.IsDeleted && x.IsActive, cancellationToken);

        var suspendedBusiness = await unitOfWork.Query<Business>()
            .CountAsync(x => !x.IsDeleted && !x.IsActive, cancellationToken);

        var trialBusiness = await unitOfWork.Query<BusinessSubscription>()
            .Where(x => !x.IsDeleted && x.Status == SubscriptionStatus.Trial)
            .Select(x => x.BusinessId)
            .Distinct()
            .CountAsync(cancellationToken);

        return ApiResponse<BusinessOverviewResponse>.Ok(new BusinessOverviewResponse
        {
            TotalBusiness = totalBusiness,
            ActiveBusiness = activeBusiness,
            TrialBusiness = trialBusiness,
            SuspendedBusiness = suspendedBusiness
        });
    }

    public async Task<ApiResponse<bool>> ActivateAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        if (!accessContext.IsPlatformAdmin)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to activate businesses",
                [new ApiError("Forbidden", "Only platform admins can activate businesses", nameof(id))]);
        }

        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (business is null)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found",
                [new ApiError("BusinessNotFound", "No business found for this id", nameof(id))]);
        }

        if (business.IsActive)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status400BadRequest,
                "Business is already active",
                [new ApiError("AlreadyActive", "This business is already active", nameof(id))]);
        }

        business.IsActive = true;
        business.UpdatedBy = SystemActor;
        business.DateUpdated = DateTime.UtcNow;

        unitOfWork.Update(business);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            userId,
            AuditAction.BusinessUpdated,
            nameof(Business),
            $"Business '{business.BusinessName}' activated.",
            businessId: business.Id,
            entityId: business.Id,
            cancellationToken: cancellationToken);

        await notificationService.CreateAsync(new CreateNotificationRequest
        {
            UserId = business.UserId,
            BusinessId = business.Id,
            Type = NotificationType.General,
            Title = "Business Activated",
            Message = $"Business '{business.BusinessName}' has been activated.",
            ActionUrl = "/business"
        }, cancellationToken);

        return ApiResponse<bool>.Ok(true, "Business activated successfully");
    }

    public async Task<ApiResponse<bool>> DeactivateAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        if (!accessContext.IsPlatformAdmin)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to deactivate businesses",
                [new ApiError("Forbidden", "Only platform admins can deactivate businesses", nameof(id))]);
        }

        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (business is null)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found",
                [new ApiError("BusinessNotFound", "No business found for this id", nameof(id))]);
        }

        if (!business.IsActive)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status400BadRequest,
                "Business is already inactive",
                [new ApiError("AlreadyInactive", "This business is already inactive", nameof(id))]);
        }

        business.IsActive = false;
        business.UpdatedBy = SystemActor;
        business.DateUpdated = DateTime.UtcNow;

        unitOfWork.Update(business);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            userId,
            AuditAction.BusinessUpdated,
            nameof(Business),
            $"Business '{business.BusinessName}' deactivated.",
            businessId: business.Id,
            entityId: business.Id,
            cancellationToken: cancellationToken);

        await notificationService.CreateAsync(new CreateNotificationRequest
        {
            UserId = business.UserId,
            BusinessId = business.Id,
            Type = NotificationType.General,
            Title = "Business Deactivated",
            Message = $"Business '{business.BusinessName}' has been deactivated.",
            ActionUrl = "/business"
        }, cancellationToken);

        return ApiResponse<bool>.Ok(true, "Business deactivated successfully");
    }

    public async Task<ApiResponse<PagedResponse<BusinessStoreResponse>>> GetStoresAsync(long businessId, PagedQueryRequest request, long userId, CancellationToken cancellationToken = default)
    {
        if (!accessContext.IsPlatformAdmin)
        {
            return ApiResponse<PagedResponse<BusinessStoreResponse>>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this business",
                [new ApiError("Forbidden", "Only platform admins can view business stores", nameof(businessId))]);
        }

        var businessExists = await unitOfWork.Query<Business>()
            .AnyAsync(x => x.Id == businessId, cancellationToken);

        if (!businessExists)
        {
            return ApiResponse<PagedResponse<BusinessStoreResponse>>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found",
                [new ApiError("BusinessNotFound", "No business found for this id", nameof(businessId))]);
        }

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = unitOfWork.Query<Store>()
            .Include(x => x.StoreCategory)
            .Where(x => x.BusinessId == businessId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(search) ||
                x.EmailAddress.ToLower().Contains(search) ||
                x.PhoneNumber.ToLower().Contains(search) ||
                x.StoreCategory.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BusinessStoreResponse
            {
                StoreId = x.Id,
                Name = x.Name,
                StoreCategory = x.StoreCategory.Name,
                EmailAddress = x.EmailAddress,
                PhoneNumber = x.PhoneNumber,
                Address = x.Address,
                City = x.City,
                State = x.State,
                Country = x.Country,
                IsActive = x.IsActive,
                CreatedAt = x.DateCreated
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResponse<BusinessStoreResponse>>.Ok(new PagedResponse<BusinessStoreResponse>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items
        });
    }

    public async Task<ApiResponse<PagedResponse<BusinessProductResponse>>> GetProductsAsync(long businessId, PagedQueryRequest request, long userId, CancellationToken cancellationToken = default)
    {
        if (!accessContext.IsPlatformAdmin)
        {
            return ApiResponse<PagedResponse<BusinessProductResponse>>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this business",
                [new ApiError("Forbidden", "Only platform admins can view business products", nameof(businessId))]);
        }

        var businessExists = await unitOfWork.Query<Business>()
            .AnyAsync(x => x.Id == businessId, cancellationToken);

        if (!businessExists)
        {
            return ApiResponse<PagedResponse<BusinessProductResponse>>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found",
                [new ApiError("BusinessNotFound", "No business found for this id", nameof(businessId))]);
        }

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = unitOfWork.Query<Product>()
            .Include(x => x.Store)
            .Include(x => x.ProductCategory)
            .Where(x => x.Store.BusinessId == businessId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(search) ||
                x.ProductCategory.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BusinessProductResponse
            {
                ProductId = x.Id,
                StoreId = x.StoreId,
                Name = x.Name,
                Price = x.Price,
                Quantity = x.Quantity,
                ProductCategory = x.ProductCategory.Name,
                IsActive = x.IsActive,
                CreatedAt = x.DateCreated
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResponse<BusinessProductResponse>>.Ok(new PagedResponse<BusinessProductResponse>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items
        });
    }

    /// <summary>
    /// Get comprehensive business summary for dashboard with metrics on businesses and stores
    /// </summary>
    public async Task<ApiResponse<BusinessSummaryDashboardResponse>> GetBusinessSummaryDashboardAsync(CancellationToken cancellationToken = default)
    {
        if (!accessContext.IsPlatformAdmin)
        {
            return ApiResponse<BusinessSummaryDashboardResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to dashboard data",
                [new ApiError("Forbidden", "Only platform admins can view dashboard data")]);
        }

        var totalBusinesses = await unitOfWork.Query<Business>()
            .CountAsync(x => !x.IsDeleted, cancellationToken);

        var activeBusinesses = await unitOfWork.Query<Business>()
            .CountAsync(x => !x.IsDeleted && x.IsActive, cancellationToken);

        var suspendedBusinesses = await unitOfWork.Query<Business>()
            .CountAsync(x => !x.IsDeleted && !x.IsActive, cancellationToken);

        var trialBusinesses = await unitOfWork.Query<BusinessSubscription>()
            .Where(x => !x.IsDeleted && x.Status == SubscriptionStatus.Trial)
            .Select(x => x.BusinessId)
            .Distinct()
            .CountAsync(cancellationToken);

        var paidBusinesses = await unitOfWork.Query<BusinessSubscription>()
            .Where(x => !x.IsDeleted && x.Status == SubscriptionStatus.Active)
            .Select(x => x.BusinessId)
            .Distinct()
            .CountAsync(cancellationToken);

        var totalStores = await unitOfWork.Query<Store>()
            .CountAsync(x => !x.IsDeleted, cancellationToken);

        var activeStores = await unitOfWork.Query<Store>()
            .CountAsync(x => !x.IsDeleted && x.IsActive, cancellationToken);

        var averageStoresPerBusiness = totalBusinesses > 0 ? (decimal)totalStores / totalBusinesses : 0;

        return ApiResponse<BusinessSummaryDashboardResponse>.Ok(new BusinessSummaryDashboardResponse
        {
            TotalBusinesses = totalBusinesses,
            ActiveBusinesses = activeBusinesses,
            SuspendedBusinesses = suspendedBusinesses,
            TrialBusinesses = trialBusinesses,
            PaidBusinesses = paidBusinesses,
            TotalStores = totalStores,
            ActiveStores = activeStores,
            AverageStoresPerBusiness = averageStoresPerBusiness
        }, "Business summary retrieved successfully");
    }

    /// <summary>
    /// Get user statistics for dashboard with optional date range filtering
    /// </summary>
    public async Task<ApiResponse<UserStatisticsDashboardResponse>> GetUserStatisticsDashboardAsync(DashboardAnalyticsRequest request, CancellationToken cancellationToken = default)
    {
        if (!accessContext.IsPlatformAdmin)
        {
            return ApiResponse<UserStatisticsDashboardResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to dashboard data",
                [new ApiError("Forbidden", "Only platform admins can view dashboard data")]);
        }

        var fromDate = request.FromDate ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = request.ToDate ?? DateTime.UtcNow;

        var totalUsers = await unitOfWork.Query<User>()
            .CountAsync(x => !x.IsDeleted, cancellationToken);

        var activeUsers = await unitOfWork.Query<User>()
            .CountAsync(x => !x.IsDeleted && x.IsActive, cancellationToken);

        var inactiveUsers = await unitOfWork.Query<User>()
            .CountAsync(x => !x.IsDeleted && !x.IsActive, cancellationToken);

        var adminUsers = await unitOfWork.Query<User>()
            .Include(x => x.Role)
            .CountAsync(x => !x.IsDeleted && x.Role != null && x.Role.Name.ToLower() == "admin", cancellationToken);

        var businessOwnerUsers = await unitOfWork.Query<Business>()
            .Select(x => x.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var staffUsers = await unitOfWork.Query<Staff>()
            .CountAsync(x => !x.IsDeleted, cancellationToken);

        var newUsersInPeriod = await unitOfWork.Query<User>()
            .CountAsync(x => !x.IsDeleted && x.DateCreated >= fromDate && x.DateCreated <= toDate, cancellationToken);

        return ApiResponse<UserStatisticsDashboardResponse>.Ok(new UserStatisticsDashboardResponse
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            InactiveUsers = inactiveUsers,
            AdminUsers = adminUsers,
            BusinessOwnerUsers = businessOwnerUsers,
            StaffUsers = staffUsers,
            NewUsersInPeriod = newUsersInPeriod,
            FromDate = fromDate,
            ToDate = toDate
        }, "User statistics retrieved successfully");
    }

    /// <summary>
    /// Get MRR analytics for dashboard with plan breakdown and optional date range filtering
    /// </summary>
    public async Task<ApiResponse<MrrAnalyticsDashboardResponse>> GetMrrAnalyticsDashboardAsync(DashboardAnalyticsRequest request, CancellationToken cancellationToken = default)
    {
        if (!accessContext.IsPlatformAdmin)
        {
            return ApiResponse<MrrAnalyticsDashboardResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to dashboard data",
                [new ApiError("Forbidden", "Only platform admins can view dashboard data")]);
        }

        var fromDate = request.FromDate ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = request.ToDate ?? DateTime.UtcNow;

        // Get all active subscriptions (considering date range for new subscriptions)
        var activeSubscriptions = await unitOfWork.Query<BusinessSubscription>()
            .Include(x => x.SubscriptionPlan)
            .Where(x => !x.IsDeleted && x.Status == SubscriptionStatus.Active && x.PlanStartDate <= toDate)
            .ToListAsync(cancellationToken);

        // Get trial subscriptions
        var trialSubscriptions = await unitOfWork.Query<BusinessSubscription>()
            .Where(x => !x.IsDeleted && x.Status == SubscriptionStatus.Trial)
            .CountAsync(cancellationToken);

        // Get cancelled subscriptions in date range
        var cancelledSubscriptions = await unitOfWork.Query<BusinessSubscription>()
            .Where(x => !x.IsDeleted && x.Status == SubscriptionStatus.Cancelled && x.PlanEndDate >= fromDate && x.PlanEndDate <= toDate)
            .CountAsync(cancellationToken);

        // Calculate MRR from active subscriptions
        decimal totalMrr = 0;
        decimal monthlySalesMrr = 0;
        decimal annualSalesMrr = 0;

        foreach (var subscription in activeSubscriptions)
        {
            if (subscription.SubscriptionPlan?.MonthlyPrice > 0)
            {
                monthlySalesMrr += subscription.SubscriptionPlan.MonthlyPrice;
            }

            if (subscription.SubscriptionPlan?.AnnualPrice > 0)
            {
                annualSalesMrr += subscription.SubscriptionPlan.AnnualPrice / 12;
            }
        }

        totalMrr = monthlySalesMrr + annualSalesMrr;

        // Group by plan for breakdown
        var mrrByPlanList = activeSubscriptions
            .GroupBy(x => new { x.SubscriptionPlan?.Id, x.SubscriptionPlan?.Name })
            .Select(g =>
            {
                decimal planMrr = 0;
                var monthlyCount = g.Count(x => x.SubscriptionPlan?.AnnualPrice == 0 || x.SubscriptionPlan?.MonthlyPrice > 0);
                var annualCount = g.Count(x => x.SubscriptionPlan?.AnnualPrice > 0);

                if (monthlyCount > 0 && g.First().SubscriptionPlan?.MonthlyPrice > 0)
                    planMrr += g.First().SubscriptionPlan.MonthlyPrice * monthlyCount;

                if (annualCount > 0 && g.First().SubscriptionPlan?.AnnualPrice > 0)
                    planMrr += (g.First().SubscriptionPlan.AnnualPrice / 12) * annualCount;

                return new MrrByPlanBreakdown
                {
                    PlanName = g.Key.Name ?? "Unknown",
                    Mrr = planMrr,
                    SubscriptionCount = g.Count(),
                    PercentageOfTotalMrr = totalMrr > 0 ? (planMrr / totalMrr) * 100 : 0
                };
            })
            .OrderByDescending(x => x.Mrr)
            .ToList();

        return ApiResponse<MrrAnalyticsDashboardResponse>.Ok(new MrrAnalyticsDashboardResponse
        {
            TotalMrr = Math.Round(totalMrr, 2),
            MonthlySalesMrr = Math.Round(monthlySalesMrr, 2),
            AnnualSalesMrr = Math.Round(annualSalesMrr, 2),
            ActiveSubscriptions = activeSubscriptions.Count,
            TrialSubscriptions = trialSubscriptions,
            CancelledSubscriptions = cancelledSubscriptions,
            MrrByPlan = mrrByPlanList,
            FromDate = fromDate,
            ToDate = toDate
        }, "MRR analytics retrieved successfully");
    }
}
