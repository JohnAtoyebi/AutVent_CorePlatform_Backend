using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class StoreService(IUnitOfWork unitOfWork) : IStoreService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<CreateStoreResponse>> CreateAsync(CreateStoreRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var storeName = request.Name.Trim();
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

        if (business.UserId != userId)
        {
            return ApiResponse<CreateStoreResponse>.Failed(
                StatusCodes.Status409Conflict,
                "Business does not belong to the current user",
                [new ApiError("UnauthorizedBusiness", "The business does not belong to the current user", nameof(request.BusinessId))]);
        }

        var storeNameExists = await unitOfWork.Query<Store>()
            .AnyAsync(x => x.Name.ToLower() == storeName.ToLower() && x.BusinessId == business.Id, cancellationToken);

        if (storeNameExists)
        {
            return ApiResponse<CreateStoreResponse>.Failed(
                StatusCodes.Status409Conflict,
                "Store name already exists for this business",
                [new ApiError("DuplicateStoreName", "Store name already exists for this business", nameof(request.Name))]);
        }

        var storeCategory = await unitOfWork.Query<StoreCategory>()
            .FirstOrDefaultAsync(x => x.Id == request.StoreCategoryId, cancellationToken);

        if (storeCategory == null)
        {
            return ApiResponse<CreateStoreResponse>.Failed(
                StatusCodes.Status409Conflict,
                "Store category doesn't exists",
                [new ApiError("InvalidStoreCategory", "Store category doesn't exists", nameof(request.StoreCategoryId))]);
        }

        var store = new Store
        {
            Name = storeName,
            EmailAddress = normalizedEmail,
            PhoneNumber = normalizedPhone,
            Address = request.Address?.Trim(),
            City = request.City?.Trim(),
            State = request.State?.Trim(),
            Country = request.Country?.Trim(),
            BusinessId = business.Id,
            StoreCategoryId = storeCategory.Id,
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = now
        };

        await unitOfWork.CreateAsync(store, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CreateStoreResponse>.Created(MapToResponse(store, storeCategory.Name, null, []), "Store created successfully");
    }

    public async Task<ApiResponse<CreateStoreResponse>> GetByIdAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var store = await unitOfWork.Query<Store>()
            .Include(x => x.Business)
            .Include(x => x.StoreCategory)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (store is null)
        {
            return ApiResponse<CreateStoreResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Store not found",
                [new ApiError("StoreNotFound", "No store found for this id", nameof(id))]);
        }

        if (store.Business.UserId != userId)
        {
            return ApiResponse<CreateStoreResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this store",
                [new ApiError("UnauthorizedStore", "This store does not belong to your business", nameof(id))]);
        }

        var bankAccounts = await GetBankAccountsAsync(store.BusinessId, cancellationToken);
        return ApiResponse<CreateStoreResponse>.Ok(MapToResponse(store, store.StoreCategory.Name, store.Business.LogoUrl, bankAccounts));
    }

    public async Task<ApiResponse<PagedResponse<CreateStoreResponse>>> GetAllAsync(PagedQueryRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = unitOfWork.Query<Store>()
            .Include(x => x.Business)
            .Include(x => x.StoreCategory)
            .Where(x => x.Business.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(search) ||
                x.EmailAddress.ToLower().Contains(search) ||
                x.PhoneNumber.ToLower().Contains(search) ||
                x.StoreCategory.Name.ToLower().Contains(search));
        }

        if (request.Filters is not null)
        {
            if (request.Filters.TryGetValue("businessId", out var businessIdFilter) && long.TryParse(businessIdFilter, out var businessId))
            {
                query = query.Where(x => x.BusinessId == businessId);
            }

            if (request.Filters.TryGetValue("storeCategory", out var categoryFilter) && !string.IsNullOrWhiteSpace(categoryFilter))
            {
                var category = categoryFilter.Trim().ToLower();
                query = query.Where(x => x.StoreCategory.Name.ToLower() == category);
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
            .Select(x => new CreateStoreResponse
            {
                StoreId = x.Id,
                BusinessId = x.BusinessId,
                Name = x.Name,
                StoreCategory = x.StoreCategory.Name,
                EmailAddress = x.EmailAddress,
                PhoneNumber = x.PhoneNumber,
                Address = x.Address,
                City = x.City,
                State = x.State,
                Country = x.Country,
                LogoUrl = x.Business.LogoUrl
            })
            .ToListAsync(cancellationToken);

        var paged = new PagedResponse<CreateStoreResponse>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items
        };

        return ApiResponse<PagedResponse<CreateStoreResponse>>.Ok(paged);
    }

    private static CreateStoreResponse MapToResponse(Store store, string storeCategory, string? logoUrl, List<BankAccountResponse> bankAccounts) => new()
    {
        StoreId = store.Id,
        BusinessId = store.BusinessId,
        Name = store.Name,
        StoreCategory = storeCategory,
        EmailAddress = store.EmailAddress,
        PhoneNumber = store.PhoneNumber,
        Address = store.Address,
        City = store.City,
        State = store.State,
        Country = store.Country,
        LogoUrl = logoUrl,
        BankAccounts = bankAccounts
    };

    private async Task<List<BankAccountResponse>> GetBankAccountsAsync(long businessId, CancellationToken cancellationToken)
    {
        return await unitOfWork.Query<BusinessBankAccount>()
            .Where(b => b.BusinessId == businessId && !b.IsDeleted)
            .OrderByDescending(b => b.IsDefault)
            .ThenBy(b => b.Id)
            .Select(b => new BankAccountResponse
            {
                Id = b.Id,
                BankName = b.BankName,
                AccountNumber = b.AccountNumber,
                AccountName = b.AccountName,
                SortCode = b.SortCode,
                IsDefault = b.IsDefault
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ApiResponse<CreateStoreResponse>> UpdateAsync(long id, UpdateStoreRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var store = await unitOfWork.Query<Store>()
            .Include(x => x.Business)
            .Include(x => x.StoreCategory)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (store is null)
            return ApiResponse<CreateStoreResponse>.Failed(StatusCodes.Status404NotFound, "Store not found",
                [new ApiError("StoreNotFound", "No store found for this id", nameof(id))]);

        if (store.Business.UserId != userId)
            return ApiResponse<CreateStoreResponse>.Failed(StatusCodes.Status403Forbidden, "You do not have access to this store",
                [new ApiError("UnauthorizedStore", "This store does not belong to your business", nameof(id))]);

        if (request.Name is not null)
        {
            var nameTaken = await unitOfWork.Query<Store>()
                .AnyAsync(x => x.Name.ToLower() == request.Name.Trim().ToLower() && x.BusinessId == store.BusinessId && x.Id != id, cancellationToken);
            if (nameTaken)
                return ApiResponse<CreateStoreResponse>.Failed(StatusCodes.Status409Conflict, "Store name already in use",
                    [new ApiError("DuplicateStoreName", "Another store in this business already has that name", nameof(request.Name))]);
            store.Name = request.Name.Trim();
        }

        if (request.StoreCategoryId.HasValue)
        {
            var category = await unitOfWork.Query<StoreCategory>()
                .FirstOrDefaultAsync(x => x.Id == request.StoreCategoryId.Value, cancellationToken);
            if (category is null)
                return ApiResponse<CreateStoreResponse>.Failed(StatusCodes.Status400BadRequest, "Store category not found",
                    [new ApiError("InvalidStoreCategory", "Store category not found", nameof(request.StoreCategoryId))]);
            store.StoreCategoryId = category.Id;
            store.StoreCategory = category;
        }

        if (request.EmailAddress is not null) store.EmailAddress = request.EmailAddress.Trim().ToLowerInvariant();
        if (request.PhoneNumber is not null)  store.PhoneNumber  = request.PhoneNumber.Trim();
        if (request.Address is not null)      store.Address      = request.Address.Trim();
        if (request.City is not null)         store.City         = request.City.Trim();
        if (request.State is not null)        store.State        = request.State.Trim();
        if (request.Country is not null)      store.Country      = request.Country.Trim();

        store.UpdatedBy = SystemActor;
        store.DateUpdated = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var bankAccounts = await GetBankAccountsAsync(store.BusinessId, cancellationToken);
        return ApiResponse<CreateStoreResponse>.Ok(MapToResponse(store, store.StoreCategory.Name, store.Business.LogoUrl, bankAccounts), "Store updated successfully");
    }
}
