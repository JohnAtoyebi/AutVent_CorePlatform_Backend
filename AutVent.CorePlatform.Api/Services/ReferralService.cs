using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class ReferralService(IUnitOfWork unitOfWork, IAccessContext accessContext) : IReferralService
{
    public async Task<ApiResponse<ValidateReferralCodeResponse>> ValidateReferralCodeAsync(string referralCode, CancellationToken cancellationToken = default)
    {
        var normalized = referralCode.Trim().ToUpperInvariant();

        var referrer = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.ReferralCode == normalized, cancellationToken);

        if (referrer is null)
        {
            return ApiResponse<ValidateReferralCodeResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Referral code not found",
                [new ApiError("InvalidReferralCode", "The referral code provided is not valid", nameof(referralCode))]);
        }

        var response = new ValidateReferralCodeResponse
        {
            ReferralCode = normalized,
            IsValid = true,
            ReferrerName = referrer.FullName
        };

        return ApiResponse<ValidateReferralCodeResponse>.Ok(response, "Referral code is valid");
    }

    /// <summary>
    /// Get businesses that were referred by a specific user, including their subscription details
    /// </summary>
    public async Task<ApiResponse<PagedResponse<ReferredBusinessResponse>>> GetReferredBusinessesAsync(
        long referrerId,
        PagedQueryRequest request,
        long userId,
        CancellationToken cancellationToken = default)
    {
        // Authorization: Users can only view their own referrals, unless they're platform admin
        if (!accessContext.IsPlatformAdmin && referrerId != userId)
        {
            return ApiResponse<PagedResponse<ReferredBusinessResponse>>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have permission to view these referrals",
                [new ApiError("Forbidden", "Only platform admins can view other users' referrals")]);
        }

        // Verify the referrer exists
        var referrer = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.Id == referrerId && !x.IsDeleted, cancellationToken);

        if (referrer is null)
        {
            return ApiResponse<PagedResponse<ReferredBusinessResponse>>.Failed(
                StatusCodes.Status404NotFound,
                "Referrer user not found",
                [new ApiError("ReferrerNotFound", "The referrer does not exist", nameof(referrerId))]);
        }

        var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 10;
        var skip = (pageNumber - 1) * pageSize;

        // Get all referral records for this user
        var referralRecords = await unitOfWork.Query<ReferralRecord>()
            .Where(x => x.ReferrerId == referrerId && !x.IsDeleted)
            .Select(x => x.ReferredUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (referralRecords.Count == 0)
        {
            return ApiResponse<PagedResponse<ReferredBusinessResponse>>.Ok(
                new PagedResponse<ReferredBusinessResponse>
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = 0,
                    TotalPages = 0,
                    Items = []
                },
                "No referred businesses found");
        }

        // Get businesses for referred users
        var businessesQuery = unitOfWork.Query<Business>()
            .Include(x => x.User)
            .Include(x => x.Stores)
            .Where(x => referralRecords.Contains(x.UserId) && !x.IsDeleted);

        var totalCount = await businessesQuery.CountAsync(cancellationToken);

        var businesses = await businessesQuery
            .OrderByDescending(x => x.DateCreated)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var businessIds = businesses.Select(x => x.Id).ToList();

        // Get subscription information for these businesses
        var subscriptions = await unitOfWork.Query<BusinessSubscription>()
            .Include(x => x.SubscriptionPlan)
            .Where(x => businessIds.Contains(x.BusinessId) && !x.IsDeleted)
            .GroupBy(x => x.BusinessId)
            .Select(g => new { BusinessId = g.Key, ActiveSubscription = g.OrderByDescending(x => x.Id).FirstOrDefault() })
            .ToListAsync(cancellationToken);

        var subscriptionDict = subscriptions.ToDictionary(x => x.BusinessId, x => x.ActiveSubscription);

        // Get store IDs for these businesses
        var storeIds = await unitOfWork.Query<Store>()
            .Where(x => businessIds.Contains(x.BusinessId) && !x.IsDeleted && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        // Get product count for each store
        var productCounts = await unitOfWork.Query<Product>()
            .Where(x => storeIds.Contains(x.StoreId) && !x.IsDeleted && x.IsActive)
            .GroupBy(x => x.StoreId)
            .Select(g => new { StoreId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var productCountDict = productCounts.ToDictionary(x => x.StoreId, x => x.Count);

        // Calculate total product count per business
        var totalProductsByBusiness = new Dictionary<long, long>();
        foreach (var business in businesses)
        {
            var storesForBusiness = business.Stores?.Where(s => !s.IsDeleted).Select(s => s.Id).ToList() ?? [];
            long totalProducts = 0;
            foreach (var storeId in storesForBusiness)
            {
                if (productCountDict.TryGetValue(storeId, out var count))
                {
                    totalProducts += count;
                }
            }
            totalProductsByBusiness[business.Id] = totalProducts;
        }

        // Build response
        var responses = businesses.Select(business =>
        {
            var hasSubscription = subscriptionDict.TryGetValue(business.Id, out var subscription);
            var storeCount = business.Stores?.Count(s => !s.IsDeleted) ?? 0;
            var productCount = totalProductsByBusiness.TryGetValue(business.Id, out var count) ? count : 0;

            return new ReferredBusinessResponse
            {
                BusinessId = business.Id,
                BusinessName = business.BusinessName,
                BusinessEmail = business.Email,
                PhoneNumber = business.PhoneNumber,
                Website = business.Website,
                Country = business.Country,
                City = business.City,
                State = business.State,
                IsActive = business.IsActive,
                CreatedDate = business.DateCreated,
                OwnerName = business.User?.FullName ?? "Unknown",
                OwnerEmail = business.User?.EmailAddress ?? "Unknown",
                SubscriptionStatus = hasSubscription ? subscription?.Status.ToString() : null,
                SubscriptionPlanName = hasSubscription ? subscription?.SubscriptionPlan?.Name : null,
                MonthlyPrice = hasSubscription ? subscription?.SubscriptionPlan?.MonthlyPrice : null,
                AnnualPrice = hasSubscription ? subscription?.SubscriptionPlan?.AnnualPrice : null,
                PlanStartDate = hasSubscription ? subscription?.PlanStartDate : null,
                PlanEndDate = hasSubscription ? subscription?.PlanEndDate : null,
                StoreCount = storeCount,
                ProductCount = productCount
            };
        }).ToList();

        return ApiResponse<PagedResponse<ReferredBusinessResponse>>.Ok(
            new PagedResponse<ReferredBusinessResponse>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Items = responses
            },
            "Referred businesses retrieved successfully");
    }
}
