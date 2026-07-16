using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class CustomerService(IUnitOfWork unitOfWork) : ICustomerService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<CustomerResponse>> CreateAsync(CreateCustomerRequest request, long userId, long storeId, CancellationToken cancellationToken = default)
    {
        // Validate store exists and belongs to the user
        var store = await unitOfWork.Query<Store>()
            .Include(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == storeId, cancellationToken);

        if (store is null)
        {
            return ApiResponse<CustomerResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Store not found",
                [new ApiError("StoreNotFound", "No store found for this id", nameof(storeId))]);
        }

        if (store.Business.UserId != userId)
        {
            return ApiResponse<CustomerResponse>.Failed(
                StatusCodes.Status409Conflict,
                "Store does not belong to the current user",
                [new ApiError("UnauthorizedStore", "The store does not belong to the current user", nameof(storeId))]);
        }

        var normalizedPhone = request.PhoneNumber.Trim();
        var normalizedEmail = request.Email?.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        // Check if phone number already exists in this store
        var phoneExists = await unitOfWork.Query<Customer>()
            .AnyAsync(x => x.PhoneNumber == normalizedPhone && x.StoreId == storeId, cancellationToken);

        if (phoneExists)
        {
            return ApiResponse<CustomerResponse>.Failed(
                StatusCodes.Status409Conflict,
                "Customer with this phone number already exists in this store",
                [new ApiError("DuplicatePhone", "Phone number already exists in this store", nameof(request.PhoneNumber))]);
        }

        // Check if email already exists in this store (if provided)
        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            var emailExists = await unitOfWork.Query<Customer>()
                .AnyAsync(x => x.Email != null && x.Email.ToLower() == normalizedEmail && x.StoreId == storeId, cancellationToken);

            if (emailExists)
            {
                return ApiResponse<CustomerResponse>.Failed(
                    StatusCodes.Status409Conflict,
                    "Customer with this email already exists in this store",
                    [new ApiError("DuplicateEmail", "Email already exists in this store", nameof(request.Email))]);
            }
        }

        var customer = new Customer
        {
            FullName = request.FullName.Trim(),
            PhoneNumber = normalizedPhone,
            Email = normalizedEmail,
            Address = request.Address?.Trim(),
            StoreId = storeId,
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = now
        };

        await unitOfWork.CreateAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CustomerResponse>.Created(MapToResponse(customer), "Customer created successfully");
    }

    public async Task<ApiResponse<CustomerResponse>> GetByIdAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var customer = await unitOfWork.Query<Customer>()
            .Include(x => x.Store)
            .ThenInclude(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (customer is null)
        {
            return ApiResponse<CustomerResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Customer not found",
                [new ApiError("CustomerNotFound", "No customer found for this id", nameof(id))]);
        }

        if (customer.Store.Business.UserId != userId)
        {
            return ApiResponse<CustomerResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this customer",
                [new ApiError("UnauthorizedCustomer", "This customer does not belong to your business", nameof(id))]);
        }

        return ApiResponse<CustomerResponse>.Ok(MapToResponse(customer));
    }

    public async Task<ApiResponse<PagedResponse<CustomerResponse>>> GetAllAsync(PagedQueryRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = unitOfWork.Query<Customer>()
            .Include(x => x.Store)
            .ThenInclude(x => x.Business)
            .Where(x => x.Store.Business.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.FullName.ToLower().Contains(search) ||
                x.PhoneNumber.Contains(search) ||
                (x.Email != null && x.Email.ToLower().Contains(search)));
        }

        if (request.Filters is not null)
        {
            if (request.Filters.TryGetValue("storeId", out var storeIdFilter) && long.TryParse(storeIdFilter, out var storeId))
            {
                query = query.Where(x => x.StoreId == storeId);
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
            .Select(x => MapToResponse(x))
            .ToListAsync(cancellationToken);

        var paged = new PagedResponse<CustomerResponse>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items
        };

        return ApiResponse<PagedResponse<CustomerResponse>>.Ok(paged);
    }

    public async Task<ApiResponse<CustomerResponse>> UpdateAsync(long id, CreateCustomerRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var customer = await unitOfWork.Query<Customer>()
            .Include(x => x.Store)
            .ThenInclude(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (customer is null)
        {
            return ApiResponse<CustomerResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Customer not found",
                [new ApiError("CustomerNotFound", "No customer found for this id", nameof(id))]);
        }

        if (customer.Store.Business.UserId != userId)
        {
            return ApiResponse<CustomerResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this customer",
                [new ApiError("UnauthorizedCustomer", "This customer does not belong to your business", nameof(id))]);
        }

        var normalizedPhone = request.PhoneNumber.Trim();
        var normalizedEmail = request.Email?.Trim().ToLowerInvariant();

        // Check if new phone number already exists in this store (if changed)
        if (customer.PhoneNumber != normalizedPhone)
        {
            var phoneExists = await unitOfWork.Query<Customer>()
                .AnyAsync(x => x.PhoneNumber == normalizedPhone && x.StoreId == customer.StoreId && x.Id != id, cancellationToken);

            if (phoneExists)
            {
                return ApiResponse<CustomerResponse>.Failed(
                    StatusCodes.Status409Conflict,
                    "Customer with this phone number already exists in this store",
                    [new ApiError("DuplicatePhone", "Phone number already exists in this store", nameof(request.PhoneNumber))]);
            }
        }

        // Check if new email already exists in this store (if changed and provided)
        if (!string.IsNullOrWhiteSpace(normalizedEmail) && customer.Email?.ToLower() != normalizedEmail)
        {
            var emailExists = await unitOfWork.Query<Customer>()
                .AnyAsync(x => x.Email != null && x.Email.ToLower() == normalizedEmail && x.StoreId == customer.StoreId && x.Id != id, cancellationToken);

            if (emailExists)
            {
                return ApiResponse<CustomerResponse>.Failed(
                    StatusCodes.Status409Conflict,
                    "Customer with this email already exists in this store",
                    [new ApiError("DuplicateEmail", "Email already exists in this store", nameof(request.Email))]);
            }
        }

        customer.FullName = request.FullName.Trim();
        customer.PhoneNumber = normalizedPhone;
        customer.Email = normalizedEmail;
        customer.Address = request.Address?.Trim();
        customer.DateUpdated = DateTime.UtcNow;
        customer.UpdatedBy = SystemActor;

        unitOfWork.Update(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CustomerResponse>.Ok(MapToResponse(customer), "Customer updated successfully");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var customer = await unitOfWork.Query<Customer>()
            .Include(x => x.Store)
            .ThenInclude(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (customer is null)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status404NotFound,
                "Customer not found",
                [new ApiError("CustomerNotFound", "No customer found for this id", nameof(id))]);
        }

        if (customer.Store.Business.UserId != userId)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this customer",
                [new ApiError("UnauthorizedCustomer", "This customer does not belong to your business", nameof(id))]);
        }

        unitOfWork.Delete(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Customer deleted successfully");
    }

    private static CustomerResponse MapToResponse(Customer customer) => new()
    {
        CustomerId = customer.Id,
        FullName = customer.FullName,
        PhoneNumber = customer.PhoneNumber,
        Email = customer.Email,
        Address = customer.Address,
        StoreId = customer.StoreId
    };
}
