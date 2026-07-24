using AutVent.CorePlatform.Api.Common.Email;
using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Infrastructure.Email;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AutVent.CorePlatform.Api.Services;

public sealed class BusinessService(IUnitOfWork unitOfWork, IEmailProvider emailProvider, IOptions<EmailOptions> emailOptions, IAuditLogService auditLogService, INotificationService notificationService) : IBusinessService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<CreateBusinessResponse>> CreateAsync(CreateBusinessRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var businessName = request.Name.Trim();
        var now = DateTime.UtcNow;

        var user = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return ApiResponse<CreateBusinessResponse>.Failed(
                StatusCodes.Status404NotFound,
                "User not found",
                [new ApiError("UserNotFound", "No user found for this id", "userId")]);
        }

        if (!user.IsActive)
        {
            return ApiResponse<CreateBusinessResponse>.Failed(
                StatusCodes.Status409Conflict,
                "User email is not verified",
                [new ApiError("UserNotVerified", "Verify user email before creating business", "userId")]);
        }

        var businessExists = await unitOfWork.Query<Business>()
            .AnyAsync(x => x.UserId == userId, cancellationToken);

        if (businessExists)
        {
            return ApiResponse<CreateBusinessResponse>.Failed(
                StatusCodes.Status409Conflict,
                "Business already exists for this user",
                [new ApiError("DuplicateBusiness", "Business already exists for this user", nameof(request.Name))]);
        }

        var industry = await unitOfWork.Query<BusinessIndustry>()
            .FirstOrDefaultAsync(x => x.Id == request.IndustryId, cancellationToken);

        if (industry == null)
        {
            return ApiResponse<CreateBusinessResponse>.Failed(
                StatusCodes.Status409Conflict,
                "Industry does not exists",
                [new ApiError("InvalidIndustry", "Industry does not exists", nameof(request.IndustryId))]);
        }

        var staffRange = await unitOfWork.Query<StaffRange>()
            .FirstOrDefaultAsync(x => x.Id == request.StaffRangeId, cancellationToken);

        if (staffRange is null)
        {
            return ApiResponse<CreateBusinessResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "Invalid staff range",
                [new ApiError("InvalidStaffRange", "Staff range not found", nameof(request.StaffRangeId))]);
        }

        var business = new Business
        {
            BusinessName = businessName,
            LogoUrl = request.LogoUrl?.Trim(),
            Email = request.Email?.Trim().ToLowerInvariant(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Website = request.Website?.Trim(),
            Address = request.Address?.Trim(),
            City = request.City?.Trim(),
            State = request.State?.Trim(),
            Country = request.Country?.Trim(),
            StaffRangeId = staffRange.Id,
            UserId = user.Id,
            BusinessIndustry = industry,
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = now
        };

        await unitOfWork.CreateAsync(business, cancellationToken);

        await emailProvider.SendAsync(
            EmailTemplates.BusinessWelcome(user.EmailAddress, user.FullName, businessName, emailOptions),
            cancellationToken);

        var starterPlan = await unitOfWork.Query<SubscriptionPlanDefinition>()
            .FirstAsync(x => x.Plan == SubscriptionPlan.Starter, cancellationToken);

        var subscription = new BusinessSubscription
        {
            Business = business,
            SubscriptionPlanId = starterPlan.Id,
            Status = SubscriptionStatus.Trial,
            TrialStartDate = now,
            TrialEndDate = now.AddDays(30),
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = now
        };
        await unitOfWork.CreateAsync(subscription, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await ProductCategoryService.MapDefaultsToBusinessAsync(unitOfWork, business.Id, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            userId,
            AuditAction.BusinessCreated,
            nameof(Business),
            $"Business '{businessName}' created.",
            businessId: business.Id,
            entityId: business.Id,
            cancellationToken: cancellationToken);

        await notificationService.CreateAsync(
            new CreateNotificationRequest
            {
                UserId = userId,
                BusinessId = business.Id,
                Type = NotificationType.General,
                Title = "Business Created",
                Message = $"Business '{business.BusinessName}' was created successfully.",
                ActionUrl = "/business"
            },
            cancellationToken);

        var response = MapToResponse(business, industry.Name, staffRange.Name);
        return ApiResponse<CreateBusinessResponse>.Created(response, "Business created successfully");
    }

    public async Task<ApiResponse<CreateBusinessResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .Include(x => x.BusinessIndustry)
            .Include(x => x.StaffRange)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (business is null)
        {
            return ApiResponse<CreateBusinessResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found",
                [new ApiError("BusinessNotFound", "No business found for this id", nameof(id))]);
        }

        return ApiResponse<CreateBusinessResponse>.Ok(MapToResponse(business, business.BusinessIndustry.Name, business.StaffRange.Name));
    }

    public async Task<ApiResponse<CreateBusinessResponse>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .Include(x => x.BusinessIndustry)
            .Include(x => x.StaffRange)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (business is null)
        {
            return ApiResponse<CreateBusinessResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found",
                [new ApiError("BusinessNotFound", "No business found for this user", nameof(userId))]);
        }

        return ApiResponse<CreateBusinessResponse>.Ok(MapToResponse(business, business.BusinessIndustry.Name, business.StaffRange.Name));
    }

    public async Task<ApiResponse<CreateBusinessResponse>> UpdateAsync(long id, UpdateBusinessRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .Include(x => x.BusinessIndustry)
            .Include(x => x.StaffRange)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (business is null)
        {
            return ApiResponse<CreateBusinessResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found",
                [new ApiError("BusinessNotFound", "No business found for this id", nameof(id))]);
        }

        if (business.UserId != userId)
        {
            return ApiResponse<CreateBusinessResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this business",
                [new ApiError("UnauthorizedBusiness", "This business does not belong to your account", nameof(id))]);
        }

        if (request.Name is not null)
            business.BusinessName = request.Name.Trim();

        if (request.IndustryId.HasValue)
        {
            var industry = await unitOfWork.Query<BusinessIndustry>()
                .FirstOrDefaultAsync(x => x.Id == request.IndustryId.Value, cancellationToken);

            if (industry is null)
            {
                return ApiResponse<CreateBusinessResponse>.Failed(
                    StatusCodes.Status400BadRequest,
                    "Industry not found",
                    [new ApiError("InvalidIndustry", "Industry not found", nameof(request.IndustryId))]);
            }

            business.BusinessIndustryId = industry.Id;
            business.BusinessIndustry = industry;
        }

        if (request.StaffRangeId.HasValue)
        {
            var staffRange = await unitOfWork.Query<StaffRange>()
                .FirstOrDefaultAsync(x => x.Id == request.StaffRangeId.Value, cancellationToken);

            if (staffRange is null)
            {
                return ApiResponse<CreateBusinessResponse>.Failed(
                    StatusCodes.Status400BadRequest,
                    "Staff range not found",
                    [new ApiError("InvalidStaffRange", "Staff range not found", nameof(request.StaffRangeId))]);
            }

            business.StaffRangeId = staffRange.Id;
            business.StaffRange = staffRange;
        }

        if (request.LogoUrl is not null) business.LogoUrl = request.LogoUrl.Trim();
        if (request.Email is not null) business.Email = request.Email.Trim().ToLowerInvariant();
        if (request.PhoneNumber is not null) business.PhoneNumber = request.PhoneNumber.Trim();
        if (request.Website is not null) business.Website = request.Website.Trim();
        if (request.Address is not null) business.Address = request.Address.Trim();
        if (request.City is not null) business.City = request.City.Trim();
        if (request.State is not null) business.State = request.State.Trim();
        if (request.Country is not null) business.Country = request.Country.Trim();

        business.UpdatedBy = SystemActor;
        business.DateUpdated = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            userId,
            AuditAction.BusinessUpdated,
            nameof(Business),
            $"Business '{business.BusinessName}' updated.",
            businessId: business.Id,
            entityId: business.Id,
            cancellationToken: cancellationToken);

        await notificationService.CreateAsync(
            new CreateNotificationRequest
            {
                UserId = userId,
                BusinessId = business.Id,
                Type = NotificationType.General,
                Title = "Business Updated",
                Message = $"Business '{business.BusinessName}' details were updated.",
                ActionUrl = "/business"
            },
            cancellationToken);

        return ApiResponse<CreateBusinessResponse>.Ok(
            MapToResponse(business, business.BusinessIndustry.Name, business.StaffRange.Name),
            "Business updated successfully");
    }

    public async Task<ApiResponse<PagedResponse<CreateBusinessResponse>>> GetAllAsync(PagedQueryRequest request, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = unitOfWork.Query<Business>()
            .Include(x => x.BusinessIndustry)
            .Include(x => x.StaffRange)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.BusinessName.ToLower().Contains(search) ||
                x.StaffRange.Name.ToLower().Contains(search) ||
                x.BusinessIndustry.Name.ToLower().Contains(search));
        }

        if (request.Filters is not null)
        {
            if (request.Filters.TryGetValue("userId", out var userIdFilter) && long.TryParse(userIdFilter, out var userId))
            {
                query = query.Where(x => x.UserId == userId);
            }

            if (request.Filters.TryGetValue("industry", out var industry) && !string.IsNullOrWhiteSpace(industry))
            {
                var industryFilter = industry.Trim().ToLower();
                query = query.Where(x => x.BusinessIndustry.Name.ToLower() == industryFilter);
            }

            if (request.Filters.TryGetValue("staffRange", out var staffRange) && !string.IsNullOrWhiteSpace(staffRange))
            {
                var staffRangeFilter = staffRange.Trim().ToLower();
                query = query.Where(x => x.StaffRange.Name.ToLower() == staffRangeFilter);
            }

            if (request.Filters.TryGetValue("isActive", out var isActiveFilter) && bool.TryParse(isActiveFilter, out var isActive))
            {
                query = query.Where(x => x.IsActive == isActive);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CreateBusinessResponse
            {
                BusinessId = x.Id,
                Name = x.BusinessName,
                Industry = x.BusinessIndustry.Name,
                StaffRange = x.StaffRange.Name,
                LogoUrl = x.LogoUrl,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                Website = x.Website,
                Address = x.Address,
                City = x.City,
                State = x.State,
                Country = x.Country
            })
            .ToListAsync(cancellationToken);

        var paged = new PagedResponse<CreateBusinessResponse>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items
        };

        return ApiResponse<PagedResponse<CreateBusinessResponse>>.Ok(paged);
    }

    private static CreateBusinessResponse MapToResponse(Business business, string industry, string staffRange) => new()
    {
        BusinessId = business.Id,
        Name = business.BusinessName,
        Industry = industry,
        StaffRange = staffRange,
        LogoUrl = business.LogoUrl,
        Email = business.Email,
        PhoneNumber = business.PhoneNumber,
        Website = business.Website,
        Address = business.Address,
        City = business.City,
        State = business.State,
        Country = business.Country
    };
}
