using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class PosService(IUnitOfWork unitOfWork) : IPosService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<SaleResponse>> CreateSaleAsync(CreateSaleRequest request, long userId, long storeId, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            return ApiResponse<SaleResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "At least one item is required",
                [new ApiError("EmptyCart", "Sale must contain at least one item", nameof(request.Items))]);
        }

        if (request.PaymentMethod == SalePaymentMethod.Unknown || !Enum.IsDefined(request.PaymentMethod))
        {
            return ApiResponse<SaleResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "Invalid payment method",
                [new ApiError("InvalidPaymentMethod", "Payment method must be one of: Cash, Transfer, Pos, Ussd, PartPayment", nameof(request.PaymentMethod))]);
        }

        if (request.DiscountType.HasValue && !Enum.IsDefined(request.DiscountType.Value))
        {
            return ApiResponse<SaleResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "Invalid discount type",
                [new ApiError("InvalidDiscountType", "Discount type must be either Percentage or Amount", nameof(request.DiscountType))]);
        }

        if (request.DiscountValue > 0 && !request.DiscountType.HasValue)
        {
            return ApiResponse<SaleResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "Invalid discount type",
                [new ApiError("InvalidDiscountType", "Discount type is required when discount value is greater than 0", nameof(request.DiscountType))]);
        }

        var discountType = request.DiscountValue > 0 ? request.DiscountType : null;
        var isPartPayment = request.PaymentMethod == SalePaymentMethod.PartPayment;
        var saleStatus = isPartPayment ? SaleStatus.Pending : SaleStatus.Completed;

        // Validate store exists and belongs to the user
        var store = await unitOfWork.Query<Store>()
            .Include(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == storeId, cancellationToken);

        if (store is null)
        {
            return ApiResponse<SaleResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Store not found",
                [new ApiError("StoreNotFound", "No store found for this id", nameof(storeId))]);
        }

        if (store.Business.UserId != userId)
        {
            return ApiResponse<SaleResponse>.Failed(
                StatusCodes.Status409Conflict,
                "Store does not belong to the current user",
                [new ApiError("UnauthorizedStore", "The store does not belong to the current user", nameof(storeId))]);
        }

        if (isPartPayment && !request.CustomerId.HasValue)
        {
            return ApiResponse<SaleResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "Customer is required for part payment",
                [new ApiError("CustomerRequired", "Part payment must be tied to an existing customer", nameof(request.CustomerId))]);
        }

        // Validate customer if provided
        Customer? customer = null;
        if (request.CustomerId.HasValue)
        {
            customer = await unitOfWork.Query<Customer>()
                .FirstOrDefaultAsync(x => x.Id == request.CustomerId.Value && x.StoreId == storeId, cancellationToken);

            if (customer is null)
            {
                return ApiResponse<SaleResponse>.Failed(
                    StatusCodes.Status404NotFound,
                    "Customer not found",
                    [new ApiError("CustomerNotFound", "Customer does not exist in this store", nameof(request.CustomerId))]);
            }
        }

        // Get all products for this sale
        var productIds = request.Items.Select(x => x.ProductId).ToArray();
        var products = await unitOfWork.Query<Product>()
            .Where(x => productIds.Contains(x.Id) && x.StoreId == storeId)
            .ToListAsync(cancellationToken);

        var productMap = products.ToDictionary(x => x.Id);

        // Validate all products exist in the store
        var missingProducts = productIds.Where(id => !productMap.ContainsKey(id)).ToArray();
        if (missingProducts.Length > 0)
        {
            return ApiResponse<SaleResponse>.Failed(
                StatusCodes.Status404NotFound,
                "One or more products not found in this store",
                missingProducts.Select(id => new ApiError("ProductNotFound", $"Product with id {id} not found in this store", nameof(CreateSaleItemRequest.ProductId))));
        }

        // Validate quantities
        var invalidQuantities = request.Items
            .Where(x => x.Quantity <= 0)
            .Select(x => new ApiError("InvalidQuantity", "Product quantity must be greater than 0", nameof(CreateSaleItemRequest.Quantity)))
            .ToList();

        if (invalidQuantities.Count > 0)
        {
            return ApiResponse<SaleResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "Invalid quantities provided",
                invalidQuantities);
        }

        var now = DateTime.UtcNow;
        var saleNumber = GenerateSaleNumber();
        decimal subTotal = 0;

        // Create sale items
        var saleItems = new List<SaleItem>();
        foreach (var item in request.Items)
        {
            var lineTotal = item.Quantity * item.UnitPrice;
            subTotal += lineTotal;

            saleItems.Add(new SaleItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = lineTotal,
                IsActive = true,
                CreatedBy = SystemActor,
                DateCreated = now
            });
        }

        var discountAmount = CalculateDiscountAmount(subTotal, discountType, request.DiscountValue);
        if (discountType == SaleDiscountType.Percentage && request.DiscountValue > 100)
        {
            return ApiResponse<SaleResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "Invalid discount percentage",
                [new ApiError("InvalidDiscountValue", "Percentage discount cannot be greater than 100", nameof(request.DiscountValue))]);
        }

        if (discountAmount > subTotal)
        {
            return ApiResponse<SaleResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "Invalid discount amount",
                [new ApiError("InvalidDiscountValue", "Discount cannot be greater than subtotal", nameof(request.DiscountValue))]);
        }

        var totalAmount = subTotal - discountAmount + request.TaxAmount;
        if (totalAmount <= 0)
        {
            return ApiResponse<SaleResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "Invalid total amount",
                [new ApiError("InvalidTotalAmount", "Total amount must be greater than 0", nameof(request.Items))]);
        }

        decimal balanceRemaining;
        decimal changeAmount;

        if (isPartPayment)
        {
            if (request.AmountPaid <= 0 || request.AmountPaid >= totalAmount)
            {
                return ApiResponse<SaleResponse>.Failed(
                    StatusCodes.Status400BadRequest,
                    "Invalid part payment amount",
                    [new ApiError("InvalidPartPayment", "For part payment, amount paid must be greater than 0 and less than total amount", nameof(request.AmountPaid))]);
            }

            balanceRemaining = totalAmount - request.AmountPaid;
            changeAmount = 0;
        }
        else
        {
            if (request.AmountPaid < totalAmount)
            {
                return ApiResponse<SaleResponse>.Failed(
                    StatusCodes.Status400BadRequest,
                    "Amount paid is less than total amount",
                    [new ApiError("InsufficientPayment", "Amount paid must be greater than or equal to total amount for this payment method", nameof(request.AmountPaid))]);
            }

            balanceRemaining = 0;
            changeAmount = request.AmountPaid - totalAmount;
        }

        // Create sale
        var sale = new Sale
        {
            SaleNumber = saleNumber,
            StoreId = storeId,
            CustomerId = request.CustomerId,
            SubTotal = subTotal,
            DiscountType = discountType,
            DiscountValue = request.DiscountValue,
            DiscountAmount = discountAmount,
            TaxAmount = request.TaxAmount,
            TotalAmount = totalAmount,
            AmountPaid = request.AmountPaid,
            BalanceRemaining = balanceRemaining,
            ChangeAmount = changeAmount,
            PaymentMethod = request.PaymentMethod,
            Status = saleStatus,
            Notes = request.Notes,
            SaleItems = saleItems,
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = now
        };

        await unitOfWork.CreateAsync(sale, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var createdSale = await unitOfWork.Query<Sale>()
            .Include(x => x.Customer)
            .Include(x => x.SaleItems)
            .ThenInclude(x => x.Product)
            .FirstAsync(x => x.Id == sale.Id, cancellationToken);

        return ApiResponse<SaleResponse>.Created(
            MapToResponse(createdSale, createdSale.Customer),
            "Sale created successfully");
    }

    public async Task<ApiResponse<SaleResponse>> GetSaleByIdAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var sale = await unitOfWork.Query<Sale>()
            .Include(x => x.Store)
            .ThenInclude(x => x.Business)
            .Include(x => x.Customer)
            .Include(x => x.SaleItems)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (sale is null)
        {
            return ApiResponse<SaleResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Sale not found",
                [new ApiError("SaleNotFound", "No sale found for this id", nameof(id))]);
        }

        if (sale.Store.Business.UserId != userId)
        {
            return ApiResponse<SaleResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this sale",
                [new ApiError("UnauthorizedSale", "This sale does not belong to your business", nameof(id))]);
        }

        return ApiResponse<SaleResponse>.Ok(MapToResponse(sale, sale.Customer));
    }

    public async Task<ApiResponse<PagedResponse<SaleResponse>>> GetSalesByStoreAsync(PagedQueryRequest request, long userId, long storeId, CancellationToken cancellationToken = default)
    {
        // Validate store belongs to user
        var store = await unitOfWork.Query<Store>()
            .Include(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == storeId, cancellationToken);

        if (store is null || store.Business.UserId != userId)
        {
            return ApiResponse<PagedResponse<SaleResponse>>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this store",
                [new ApiError("UnauthorizedStore", "This store does not belong to your business", nameof(storeId))]);
        }

        return await GetSalesAsync(request, userId, storeId, cancellationToken);
    }

    public async Task<ApiResponse<PagedResponse<SaleResponse>>> GetAllSalesAsync(PagedQueryRequest request, long userId, CancellationToken cancellationToken = default)
    {
        return await GetSalesAsync(request, userId, null, cancellationToken);
    }

    private async Task<ApiResponse<PagedResponse<SaleResponse>>> GetSalesAsync(PagedQueryRequest request, long userId, long? storeId, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = unitOfWork.Query<Sale>()
            .Include(x => x.Store)
            .ThenInclude(x => x.Business)
            .Include(x => x.Customer)
            .Include(x => x.SaleItems)
            .ThenInclude(x => x.Product)
            .Where(x => x.Store.Business.UserId == userId)
            .AsQueryable();

        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.SaleNumber.ToLower().Contains(search) ||
                (x.Customer != null && x.Customer.FullName.ToLower().Contains(search)));
        }

        if (request.Filters is not null)
        {
            if (request.Filters.TryGetValue("paymentMethod", out var paymentMethod) &&
                Enum.TryParse<SalePaymentMethod>(paymentMethod, true, out var parsedPaymentMethod) &&
                parsedPaymentMethod != SalePaymentMethod.Unknown)
            {
                query = query.Where(x => x.PaymentMethod == parsedPaymentMethod);
            }

            if (request.Filters.TryGetValue("status", out var status) &&
                Enum.TryParse<SaleStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(x => x.Status == parsedStatus);
            }

            if (request.Filters.TryGetValue("startDate", out var startDateStr) && DateTime.TryParse(startDateStr, out var startDate))
            {
                query = query.Where(x => x.DateCreated.Date >= startDate.Date);
            }

            if (request.Filters.TryGetValue("endDate", out var endDateStr) && DateTime.TryParse(endDateStr, out var endDate))
            {
                query = query.Where(x => x.DateCreated.Date <= endDate.Date);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToResponse(x, x.Customer))
            .ToListAsync(cancellationToken);

        var paged = new PagedResponse<SaleResponse>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items
        };

        return ApiResponse<PagedResponse<SaleResponse>>.Ok(paged);
    }

    private static string GenerateSaleNumber()
    {
        return $"SALE-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    }

    private static decimal CalculateDiscountAmount(decimal subTotal, SaleDiscountType? discountType, decimal discountValue)
    {
        if (discountValue <= 0 || !discountType.HasValue)
        {
            return 0;
        }

        return discountType.Value == SaleDiscountType.Percentage
            ? subTotal * (discountValue / 100)
            : discountValue;
    }

    private static SaleResponse MapToResponse(Sale sale, Customer? customer)
    {
        var saleItems = sale.SaleItems
            .Select(x => new SaleItemResponse
            {
                SaleItemId = x.Id,
                ProductId = x.ProductId,
                ProductName = x.Product.Name,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                LineTotal = x.LineTotal
            })
            .ToList();

        return new SaleResponse
        {
            SaleId = sale.Id,
            SaleNumber = sale.SaleNumber,
            StoreId = sale.StoreId,
            CustomerId = sale.CustomerId,
            CustomerName = customer?.FullName,
            SubTotal = sale.SubTotal,
            DiscountType = sale.DiscountType,
            DiscountValue = sale.DiscountValue,
            DiscountAmount = sale.DiscountAmount,
            TaxAmount = sale.TaxAmount,
            TotalAmount = sale.TotalAmount,
            AmountPaid = sale.AmountPaid,
            BalanceRemaining = sale.BalanceRemaining,
            ChangeAmount = sale.ChangeAmount,
            PaymentMethod = sale.PaymentMethod,
            Status = sale.Status,
            Notes = sale.Notes,
            Items = saleItems,
            SaleDate = sale.DateCreated
        };
    }
}
