using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class BusinessService(IUnitOfWork unitOfWork) : IBusinessService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<CreateBusinessResponse>> CreateAsync(CreateBusinessRequest request, CancellationToken cancellationToken = default)
    {
        var businessName = request.Name.Trim();
        var industryName = request.Industry.Trim();
        var staffRange = request.StaffRange.Trim();
        var now = DateTime.UtcNow;

        var user = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return ApiResponse<CreateBusinessResponse>.Failed(
                StatusCodes.Status404NotFound,
                "User not found",
                [new ApiError("UserNotFound", "No user found for this id", nameof(request.UserId))]);
        }

        if (!user.IsActive)
        {
            return ApiResponse<CreateBusinessResponse>.Failed(
                StatusCodes.Status409Conflict,
                "User email is not verified",
                [new ApiError("UserNotVerified", "Verify user email before creating business", nameof(request.UserId))]);
        }

        var businessExists = await unitOfWork.Query<Business>()
            .AnyAsync(x => x.UserId == request.UserId && x.BusinessName.ToLower() == businessName.ToLower(), cancellationToken);

        if (businessExists)
        {
            return ApiResponse<CreateBusinessResponse>.Failed(
                StatusCodes.Status409Conflict,
                "Business already exists for this user",
                [new ApiError("DuplicateBusiness", "Business name already exists for this user", nameof(request.Name))]);
        }

        var industry = await unitOfWork.Query<BusinessIndustry>()
            .FirstOrDefaultAsync(x => x.Name.ToLower() == industryName.ToLower(), cancellationToken);

        if (industry is null)
        {
            industry = new BusinessIndustry
            {
                Name = industryName,
                IsActive = true,
                CreatedBy = SystemActor,
                DateCreated = now
            };

            await unitOfWork.CreateAsync(industry, cancellationToken);
        }

        var business = new Business
        {
            BusinessName = businessName,
            StaffRange = staffRange,
            UserId = user.Id,
            BusinessIndustry = industry,
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = now
        };

        await unitOfWork.CreateAsync(business, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = MapToResponse(business, industry.Name);
        return ApiResponse<CreateBusinessResponse>.Created(response, "Business created successfully");
    }

    public async Task<ApiResponse<CreateBusinessResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .Include(x => x.BusinessIndustry)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (business is null)
        {
            return ApiResponse<CreateBusinessResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found",
                [new ApiError("BusinessNotFound", "No business found for this id", nameof(id))]);
        }

        return ApiResponse<CreateBusinessResponse>.Ok(MapToResponse(business, business.BusinessIndustry.Name));
    }

    public async Task<ApiResponse<IReadOnlyCollection<CreateBusinessResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await unitOfWork.Query<Business>()
            .Include(x => x.BusinessIndustry)
            .Select(x => new CreateBusinessResponse
            {
                BusinessId = x.Id,
                Name = x.BusinessName,
                Industry = x.BusinessIndustry.Name,
                StaffRange = x.StaffRange
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<CreateBusinessResponse>>.Ok(items);
    }

    private static CreateBusinessResponse MapToResponse(Business business, string industry) => new()
    {
        BusinessId = business.Id,
        Name = business.BusinessName,
        Industry = industry,
        StaffRange = business.StaffRange
    };
}
