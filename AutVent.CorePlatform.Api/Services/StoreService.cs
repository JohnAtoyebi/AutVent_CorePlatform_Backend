using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class StoreService(IUnitOfWork unitOfWork) : IStoreService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<CreateStoreResponse>> CreateAsync(CreateStoreRequest request, CancellationToken cancellationToken = default)
    {
        var storeName = request.Name.Trim();
        var storeCategoryName = request.StoreCategory.Trim();
        var normalizedEmail = request.EmailAddress.Trim().ToLowerInvariant();
        var normalizedPhone = request.PhoneNumber.Trim();
        var now = DateTime.UtcNow;

        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.Id == request.BusinessId, cancellationToken);

        if (business is null)
        {
            return ApiResponse<CreateStoreResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found",
                [new ApiError("BusinessNotFound", "No business found for this id", nameof(request.BusinessId))]);
        }

        var emailExists = await unitOfWork.Query<Store>()
            .AnyAsync(x => x.EmailAddress.ToLower() == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return ApiResponse<CreateStoreResponse>.Failed(
                StatusCodes.Status409Conflict,
                "Store email already exists",
                [new ApiError("DuplicateStoreEmail", "Store email address already exists", nameof(request.EmailAddress))]);
        }

        var phoneExists = await unitOfWork.Query<Store>()
            .AnyAsync(x => x.PhoneNumber == normalizedPhone, cancellationToken);

        if (phoneExists)
        {
            return ApiResponse<CreateStoreResponse>.Failed(
                StatusCodes.Status409Conflict,
                "Store phone number already exists",
                [new ApiError("DuplicateStorePhone", "Store phone number already exists", nameof(request.PhoneNumber))]);
        }

        var storeCategory = await unitOfWork.Query<StoreCategory>()
            .FirstOrDefaultAsync(x => x.Name.ToLower() == storeCategoryName.ToLower(), cancellationToken);

        if (storeCategory is null)
        {
            storeCategory = new StoreCategory
            {
                Name = storeCategoryName,
                IsActive = true,
                CreatedBy = SystemActor,
                DateCreated = now
            };

            await unitOfWork.CreateAsync(storeCategory, cancellationToken);
        }

        var store = new Store
        {
            Name = storeName,
            EmailAddress = normalizedEmail,
            PhoneNumber = normalizedPhone,
            BusinessId = business.Id,
            StoreCategory = storeCategory,
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = now
        };

        await unitOfWork.CreateAsync(store, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CreateStoreResponse>.Created(MapToResponse(store, storeCategory.Name), "Store created successfully");
    }

    public async Task<ApiResponse<CreateStoreResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var store = await unitOfWork.Query<Store>()
            .Include(x => x.StoreCategory)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (store is null)
        {
            return ApiResponse<CreateStoreResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Store not found",
                [new ApiError("StoreNotFound", "No store found for this id", nameof(id))]);
        }

        return ApiResponse<CreateStoreResponse>.Ok(MapToResponse(store, store.StoreCategory.Name));
    }

    public async Task<ApiResponse<IReadOnlyCollection<CreateStoreResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await unitOfWork.Query<Store>()
            .Include(x => x.StoreCategory)
            .Select(x => new CreateStoreResponse
            {
                StoreId = x.Id,
                BusinessId = x.BusinessId,
                Name = x.Name,
                StoreCategory = x.StoreCategory.Name,
                EmailAddress = x.EmailAddress,
                PhoneNumber = x.PhoneNumber
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<CreateStoreResponse>>.Ok(items);
    }

    private static CreateStoreResponse MapToResponse(Store store, string storeCategory) => new()
    {
        StoreId = store.Id,
        BusinessId = store.BusinessId,
        Name = store.Name,
        StoreCategory = storeCategory,
        EmailAddress = store.EmailAddress,
        PhoneNumber = store.PhoneNumber
    };
}
