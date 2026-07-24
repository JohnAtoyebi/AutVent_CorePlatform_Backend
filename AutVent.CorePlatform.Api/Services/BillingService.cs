using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class BillingService(IUnitOfWork unitOfWork, IAuditLogService auditLogService, INotificationService notificationService) : IBillingService
{
    public async Task<ApiResponse<BillingTransactionResponse>> CreateAsync(
        long userId,
        CreateBillingTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted, cancellationToken);

        if (business is null)
            return ApiResponse<BillingTransactionResponse>.Failed(
                StatusCodes.Status404NotFound, "Business not found for this user.");

        var plan = await unitOfWork.Query<SubscriptionPlanDefinition>()
            .FirstOrDefaultAsync(x => x.Id == request.SubscriptionPlanId && !x.IsDeleted, cancellationToken);

        if (plan is null)
            return ApiResponse<BillingTransactionResponse>.Failed(
                StatusCodes.Status404NotFound, "Subscription plan not found.");

        var duplicate = await unitOfWork.Query<BillingSubscriptionTransaction>()
            .AnyAsync(x => x.TransactionReference == request.TransactionReference, cancellationToken);

        if (duplicate)
            return ApiResponse<BillingTransactionResponse>.Failed(
                StatusCodes.Status409Conflict, "A transaction with this reference already exists.");

        var transaction = new BillingSubscriptionTransaction
        {
            BusinessId = business.Id,
            SubscriptionPlanId = request.SubscriptionPlanId,
            TransactionReference = request.TransactionReference,
            ProviderReference = request.ProviderReference,
            Amount = request.Amount,
            Currency = request.Currency,
            BillingCycle = request.BillingCycle,
            VerificationStatus = TransactionVerificationStatus.Pending,
            IsActive = true,
            CreatedBy = business.Id.ToString(),
            DateCreated = DateTime.UtcNow
        };

        await unitOfWork.CreateAsync(transaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<BillingTransactionResponse>.Ok(MapToResponse(transaction, plan.Name));
    }

    public async Task<ApiResponse<BillingTransactionResponse>> VerifyAsync(
        VerifyBillingTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var transaction = await unitOfWork.Query<BillingSubscriptionTransaction>()
            .Include(x => x.SubscriptionPlan)
            .FirstOrDefaultAsync(x => x.TransactionReference == request.TransactionReference, cancellationToken);

        User? businessOwner = null;
        Business? business = null;

        if (transaction is null)
            return ApiResponse<BillingTransactionResponse>.Failed(
                StatusCodes.Status404NotFound, "Transaction not found.");

        if (transaction.VerificationStatus == TransactionVerificationStatus.Verified)
            return ApiResponse<BillingTransactionResponse>.Failed(
                StatusCodes.Status409Conflict, "Transaction has already been verified.");

        var now = DateTime.UtcNow;

        transaction.VerificationStatus = request.VerificationStatus;
        transaction.FailureReason = request.FailureReason;
        transaction.VerifiedAt = request.VerificationStatus == TransactionVerificationStatus.Verified
            ? now
            : null;
        transaction.DateUpdated = now;
        transaction.UpdatedBy = transaction.BusinessId.ToString();

        unitOfWork.Update(transaction);

        if (request.VerificationStatus == TransactionVerificationStatus.Verified)
        {
            business = await unitOfWork.Query<Business>()
                .FirstOrDefaultAsync(x => x.Id == transaction.BusinessId && !x.IsDeleted, cancellationToken);

            if (business is not null)
            {
                businessOwner = await unitOfWork.Query<User>()
                    .FirstOrDefaultAsync(x => x.Id == business.UserId && !x.IsDeleted, cancellationToken);
            }

            var planEndDate = transaction.BillingCycle == BillingCycle.Annual
                ? now.AddYears(1)
                : now.AddMonths(1);

            var activeSubscriptions = await unitOfWork.Query<BusinessSubscription>()
                .Where(x => x.BusinessId == transaction.BusinessId && x.IsActive && !x.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var activeSubscription in activeSubscriptions)
            {
                activeSubscription.IsActive = false;
                activeSubscription.Status = SubscriptionStatus.Expired;
                activeSubscription.DateUpdated = now;
                activeSubscription.UpdatedBy = transaction.BusinessId.ToString();
                unitOfWork.Update(activeSubscription);
            }

            var businessSubscription = new BusinessSubscription
            {
                BusinessId = transaction.BusinessId,
                SubscriptionPlanId = transaction.SubscriptionPlanId,
                Status = SubscriptionStatus.Active,
                TrialStartDate = now,
                TrialEndDate = now,
                PlanStartDate = now,
                PlanEndDate = planEndDate,
                IsActive = true,
                CreatedBy = transaction.BusinessId.ToString(),
                DateCreated = now
            };

            await unitOfWork.CreateAsync(businessSubscription, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (request.VerificationStatus == TransactionVerificationStatus.Verified && business is not null && businessOwner is not null)
        {
            await auditLogService.LogAsync(
                businessOwner.Id,
                AuditAction.SubscriptionPaymentMade,
                nameof(BillingSubscriptionTransaction),
                $"Subscription payment verified for business '{business.BusinessName}'.",
                businessId: business.Id,
                entityId: transaction.Id,
                cancellationToken: cancellationToken);

            await notificationService.CreateAsync(
                new CreateNotificationRequest
                {
                    UserId = businessOwner.Id,
                    BusinessId = business.Id,
                    Type = NotificationType.SubscriptionUpgraded,
                    Title = "Subscription Payment Successful",
                    Message = $"Payment for the {transaction.SubscriptionPlan.Name} plan was successful.",
                    ActionUrl = "/billing/businesses/" + business.Id + "/subscriptions/active"
                },
                cancellationToken);
        }

        return ApiResponse<BillingTransactionResponse>.Ok(MapToResponse(transaction, transaction.SubscriptionPlan.Name));
    }

    public async Task<ApiResponse<BillingTransactionResponse>> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var transaction = await unitOfWork.Query<BillingSubscriptionTransaction>()
            .Include(x => x.SubscriptionPlan)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (transaction is null)
            return ApiResponse<BillingTransactionResponse>.Failed(
                StatusCodes.Status404NotFound, "Transaction not found.");

        return ApiResponse<BillingTransactionResponse>.Ok(
            MapToResponse(transaction, transaction.SubscriptionPlan.Name));
    }

    public async Task<ApiResponse<PagedResponse<BillingTransactionResponse>>> GetAllAsync(
        long userId,
        PagedQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted, cancellationToken);

        if (business is null)
            return ApiResponse<PagedResponse<BillingTransactionResponse>>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found for this user.");

        var query = unitOfWork.Query<BillingSubscriptionTransaction>()
            .Include(x => x.SubscriptionPlan)
            .Where(x => x.BusinessId == business.Id && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.TransactionReference.ToLower().Contains(search) ||
                (x.ProviderReference != null && x.ProviderReference.ToLower().Contains(search)));
        }

        if (request.Filters is not null &&
            request.Filters.TryGetValue("verificationStatus", out var statusFilter) &&
            Enum.TryParse<TransactionVerificationStatus>(statusFilter, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(x => x.VerificationStatus == parsedStatus);
        }

        if (request.Filters is not null &&
            request.Filters.TryGetValue("billingCycle", out var cycleFilter) &&
            Enum.TryParse<BillingCycle>(cycleFilter, ignoreCase: true, out var parsedCycle))
        {
            query = query.Where(x => x.BillingCycle == parsedCycle);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy?.ToLower() switch
        {
            "oldest" => query.OrderBy(x => x.Id),
            _ => query.OrderByDescending(x => x.Id)
        };

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => MapToResponse(x, x.SubscriptionPlan.Name))
            .ToListAsync(cancellationToken);

        var paged = new PagedResponse<BillingTransactionResponse>
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
            Items = items
        };

        return ApiResponse<PagedResponse<BillingTransactionResponse>>.Ok(paged);
    }

    public async Task<ApiResponse<PagedResponse<BusinessSubscriptionResponse>>> GetSubscriptionsByBusinessIdAsync(
        long businessId,
        long userId,
        PagedQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.Id == businessId && !x.IsDeleted, cancellationToken);

        if (business is null)
            return ApiResponse<PagedResponse<BusinessSubscriptionResponse>>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found.");

        if (business.UserId != userId)
            return ApiResponse<PagedResponse<BusinessSubscriptionResponse>>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this business subscriptions.");

        var query = unitOfWork.Query<BusinessSubscription>()
            .Include(x => x.SubscriptionPlan)
            .Where(x => x.BusinessId == businessId && !x.IsDeleted);

        if (request.Filters is not null &&
            request.Filters.TryGetValue("status", out var statusFilter) &&
            Enum.TryParse<SubscriptionStatus>(statusFilter, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy?.ToLower() switch
        {
            "oldest" => query.OrderBy(x => x.Id),
            _ => query.OrderByDescending(x => x.Id)
        };

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => MapToResponse(x, x.SubscriptionPlan.Name))
            .ToListAsync(cancellationToken);

        var paged = new PagedResponse<BusinessSubscriptionResponse>
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
            Items = items
        };

        return ApiResponse<PagedResponse<BusinessSubscriptionResponse>>.Ok(paged);
    }

    public async Task<ApiResponse<BusinessSubscriptionResponse>> GetActiveSubscriptionByBusinessIdAsync(
        long businessId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.Id == businessId && !x.IsDeleted, cancellationToken);

        if (business is null)
            return ApiResponse<BusinessSubscriptionResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found.");

        if (business.UserId != userId)
            return ApiResponse<BusinessSubscriptionResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this business subscriptions.");

        var activeSubscription = await unitOfWork.Query<BusinessSubscription>()
            .Include(x => x.SubscriptionPlan)
            .Where(x => x.BusinessId == businessId && x.IsActive && !x.IsDeleted)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeSubscription is null)
            return ApiResponse<BusinessSubscriptionResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Active subscription not found for this business.");

        return ApiResponse<BusinessSubscriptionResponse>.Ok(
            MapToResponse(activeSubscription, activeSubscription.SubscriptionPlan.Name));
    }

    private static BillingTransactionResponse MapToResponse(BillingSubscriptionTransaction t, string planName) =>
        new()
        {
            Id = t.Id,
            BusinessId = t.BusinessId,
            SubscriptionPlanId = t.SubscriptionPlanId,
            SubscriptionPlanName = planName,
            TransactionReference = t.TransactionReference,
            ProviderReference = t.ProviderReference,
            Amount = t.Amount,
            Currency = t.Currency,
            BillingCycle = t.BillingCycle.ToString(),
            VerificationStatus = t.VerificationStatus.ToString(),
            FailureReason = t.FailureReason,
            VerifiedAt = t.VerifiedAt,
            DateCreated = t.DateCreated,
            DateUpdated = t.DateUpdated
        };

    private static BusinessSubscriptionResponse MapToResponse(BusinessSubscription s, string planName) =>
        new()
        {
            Id = s.Id,
            BusinessId = s.BusinessId,
            SubscriptionPlanId = s.SubscriptionPlanId,
            SubscriptionPlanName = planName,
            Status = s.Status.ToString(),
            TrialStartDate = s.TrialStartDate,
            TrialEndDate = s.TrialEndDate,
            PlanStartDate = s.PlanStartDate,
            PlanEndDate = s.PlanEndDate,
            DateCreated = s.DateCreated,
            DateUpdated = s.DateUpdated
        };
}

