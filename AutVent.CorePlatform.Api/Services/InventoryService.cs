using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class InventoryService(IUnitOfWork unitOfWork) : IInventoryService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<InventorySummaryResponse>> GetSummaryAsync(InventorySummaryFilterRequest request, long userId, long storeId, CancellationToken cancellationToken = default)
    {
        var store = await unitOfWork.Query<Store>()
            .Include(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == storeId, cancellationToken);

        if (store is null)
        {
            return ApiResponse<InventorySummaryResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Store not found",
                [new ApiError("StoreNotFound", "No store found for this id", nameof(storeId))]);
        }

        if (store.Business.UserId != userId)
        {
            return ApiResponse<InventorySummaryResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this store",
                [new ApiError("UnauthorizedStore", "This store does not belong to your business", nameof(storeId))]);
        }

        var endDate = (request.EndDate ?? DateTime.UtcNow).ToUniversalTime();
        var startDate = (request.StartDate ?? endDate.AddDays(-30)).ToUniversalTime();

        if (startDate > endDate)
        {
            return ApiResponse<InventorySummaryResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "Invalid date range",
                [new ApiError("InvalidDateRange", "StartDate cannot be greater than EndDate", nameof(request.StartDate))]);
        }

        var periodDuration = endDate - startDate;
        if (periodDuration <= TimeSpan.Zero)
        {
            periodDuration = TimeSpan.FromDays(1);
        }

        var previousEndDate = startDate;
        var previousStartDate = startDate - periodDuration;

        var salesCurrentQuery = unitOfWork.Query<Sale>()
            .Where(x => x.StoreId == storeId && x.DateCreated >= startDate && x.DateCreated <= endDate);

        var salesPreviousQuery = unitOfWork.Query<Sale>()
            .Where(x => x.StoreId == storeId && x.DateCreated >= previousStartDate && x.DateCreated < previousEndDate);

        var salesCount = await salesCurrentQuery.CountAsync(cancellationToken);
        var previousSalesCount = await salesPreviousQuery.CountAsync(cancellationToken);

        var productSoldCount = await unitOfWork.Query<SaleItem>()
            .Where(x => x.Sale.StoreId == storeId && x.Sale.DateCreated >= startDate && x.Sale.DateCreated <= endDate)
            .SumAsync(x => (long?)x.Quantity, cancellationToken) ?? 0;

        var previousProductSoldCount = await unitOfWork.Query<SaleItem>()
            .Where(x => x.Sale.StoreId == storeId && x.Sale.DateCreated >= previousStartDate && x.Sale.DateCreated < previousEndDate)
            .SumAsync(x => (long?)x.Quantity, cancellationToken) ?? 0;

        var newCustomerCount = await unitOfWork.Query<Customer>()
            .Where(x => x.StoreId == storeId && x.DateCreated >= startDate && x.DateCreated <= endDate)
            .CountAsync(cancellationToken);

        var previousNewCustomerCount = await unitOfWork.Query<Customer>()
            .Where(x => x.StoreId == storeId && x.DateCreated >= previousStartDate && x.DateCreated < previousEndDate)
            .CountAsync(cancellationToken);

        var lowStockCount = await unitOfWork.Query<Product>()
            .Where(x =>
                x.StoreId == storeId &&
                x.DateCreated >= startDate &&
                x.DateCreated <= endDate &&
                x.ReorderThreshold.HasValue &&
                x.Quantity <= x.ReorderThreshold.Value)
            .CountAsync(cancellationToken);

        var previousLowStockCount = await unitOfWork.Query<Product>()
            .Where(x =>
                x.StoreId == storeId &&
                x.DateCreated >= previousStartDate &&
                x.DateCreated < previousEndDate &&
                x.ReorderThreshold.HasValue &&
                x.Quantity <= x.ReorderThreshold.Value)
            .CountAsync(cancellationToken);

        var outOfStockCount = await unitOfWork.Query<Product>()
            .Where(x => x.StoreId == storeId && x.DateCreated >= startDate && x.DateCreated <= endDate && x.Quantity == 0)
            .CountAsync(cancellationToken);

        var previousOutOfStockCount = await unitOfWork.Query<Product>()
            .Where(x => x.StoreId == storeId && x.DateCreated >= previousStartDate && x.DateCreated < previousEndDate && x.Quantity == 0)
            .CountAsync(cancellationToken);

        var summary = new InventorySummaryResponse
        {
            StoreId = storeId,
            StartDate = startDate,
            EndDate = endDate,
            ProductSoldCount = productSoldCount,
            ProductSoldPercentageIncrease = CalculatePercentageIncrease(productSoldCount, previousProductSoldCount),
            SalesCount = salesCount,
            SalesCountPercentageIncrease = CalculatePercentageIncrease(salesCount, previousSalesCount),
            NewCustomerCount = newCustomerCount,
            NewCustomerPercentageIncrease = CalculatePercentageIncrease(newCustomerCount, previousNewCustomerCount),
            LowStockCount = lowStockCount,
            LowStockPercentageIncrease = CalculatePercentageIncrease(lowStockCount, previousLowStockCount),
            OutOfStockCount = outOfStockCount,
            OutOfStockPercentageIncrease = CalculatePercentageIncrease(outOfStockCount, previousOutOfStockCount)
        };

        return ApiResponse<InventorySummaryResponse>.Ok(summary);
    }

    public async Task<ApiResponse<PagedResponse<InventoryItemResponse>>> GetItemsAsync(PagedQueryRequest request, long userId, long storeId, CancellationToken cancellationToken = default)
    {
        var store = await unitOfWork.Query<Store>()
            .Include(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == storeId, cancellationToken);

        if (store is null)
        {
            return ApiResponse<PagedResponse<InventoryItemResponse>>.Failed(
                StatusCodes.Status404NotFound,
                "Store not found",
                [new ApiError("StoreNotFound", "No store found for this id", nameof(storeId))]);
        }

        if (store.Business.UserId != userId)
        {
            return ApiResponse<PagedResponse<InventoryItemResponse>>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this store",
                [new ApiError("UnauthorizedStore", "This store does not belong to your business", nameof(storeId))]);
        }

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = unitOfWork.Query<Product>()
            .Include(x => x.ProductCategory)
            .Where(x => x.StoreId == storeId && !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(search) ||
                (x.Sku != null && x.Sku.ToLower().Contains(search)));
        }

        if (request.Filters is not null)
        {
            if (request.Filters.TryGetValue("storeIds", out var storeIdsFilter))
            {
                var parsedIds = storeIdsFilter
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => long.TryParse(s, out var v) ? v : (long?)null)
                    .Where(v => v.HasValue)
                    .Select(v => v!.Value)
                    .ToList();

                if (parsedIds.Count > 0)
                {
                    var authorizedStoreIds = await unitOfWork.Query<Store>()
                        .Where(x => parsedIds.Contains(x.Id) && x.Business.UserId == userId)
                        .Select(x => x.Id)
                        .ToListAsync(cancellationToken);

                    var unauthorizedIds = parsedIds.Except(authorizedStoreIds).ToList();
                    if (unauthorizedIds.Count > 0)
                    {
                        return ApiResponse<PagedResponse<InventoryItemResponse>>.Failed(
                            StatusCodes.Status403Forbidden,
                            "One or more store IDs do not belong to your business",
                            unauthorizedIds.Select(id => new ApiError("UnauthorizedStore", $"Store {id} does not belong to your business", "storeIds")));
                    }

                    query = query.Where(x => authorizedStoreIds.Contains(x.StoreId));
                }
            }

            if (request.Filters.TryGetValue("stockStatus", out var stockStatusFilter) &&
                Enum.TryParse<InventoryStockStatus>(stockStatusFilter, true, out var stockStatus) &&
                stockStatus != InventoryStockStatus.All)
            {
                query = stockStatus switch
                {
                    InventoryStockStatus.OutOfStock => query.Where(x => x.Quantity == 0),
                    InventoryStockStatus.LowStock   => query.Where(x => x.Quantity > 0 && x.ReorderThreshold.HasValue && x.Quantity <= x.ReorderThreshold.Value),
                    InventoryStockStatus.InStock    => query.Where(x => x.Quantity > 0 && (!x.ReorderThreshold.HasValue || x.Quantity > x.ReorderThreshold.Value)),
                    _ => query
                };
            }
            else if (request.Filters.TryGetValue("isLowStock", out var isLowStockFilter) && bool.TryParse(isLowStockFilter, out var isLowStock))
            {
                query = isLowStock
                    ? query.Where(x => x.ReorderThreshold.HasValue && x.Quantity <= x.ReorderThreshold.Value)
                    : query.Where(x => !x.ReorderThreshold.HasValue || x.Quantity > x.ReorderThreshold.Value);
            }

            if (request.Filters.TryGetValue("isActive", out var isActiveFilter) && bool.TryParse(isActiveFilter, out var isActive))
            {
                query = query.Where(x => x.IsActive == isActive);
            }

            if (request.Filters.TryGetValue("minQuantity", out var minQtyFilter) && long.TryParse(minQtyFilter, out var minQty))
            {
                query = query.Where(x => x.Quantity >= minQty);
            }

            if (request.Filters.TryGetValue("maxQuantity", out var maxQtyFilter) && long.TryParse(maxQtyFilter, out var maxQty))
            {
                query = query.Where(x => x.Quantity <= maxQty);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var sortBy = Enum.TryParse<InventorySortBy>(request.SortBy, true, out var parsedSort)
            ? parsedSort
            : InventorySortBy.Newest;

        query = sortBy switch
        {
            InventorySortBy.Oldest       => query.OrderBy(x => x.Id),
            InventorySortBy.NameAsc      => query.OrderBy(x => x.Name),
            InventorySortBy.NameDesc     => query.OrderByDescending(x => x.Name),
            InventorySortBy.QuantityAsc  => query.OrderBy(x => x.Quantity),
            InventorySortBy.QuantityDesc => query.OrderByDescending(x => x.Quantity),
            _                            => query.OrderByDescending(x => x.Id)
        };

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new InventoryItemResponse
            {
                ProductId = x.Id,
                StoreId = x.StoreId,
                Name = x.Name,
                Sku = x.Sku,
                Quantity = x.Quantity,
                ReorderThreshold = x.ReorderThreshold,
                IsLowStock = x.ReorderThreshold.HasValue && x.Quantity <= x.ReorderThreshold.Value,
                IsActive = x.IsActive,
                ProductCategory = x.ProductCategory.Name,
                CostPrice = x.CostPrice
            })
            .ToListAsync(cancellationToken);

        var paged = new PagedResponse<InventoryItemResponse>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items
        };

        return ApiResponse<PagedResponse<InventoryItemResponse>>.Ok(paged);
    }

    public async Task<ApiResponse<InventoryItemResponse>> UpdateStockAsync(long productId, UpdateInventoryStockRequest request, long userId, long storeId, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Query<Product>()
            .Include(x => x.Store)
            .ThenInclude(x => x.Business)
            .Include(x => x.ProductCategory)
            .FirstOrDefaultAsync(x => x.Id == productId && x.StoreId == storeId, cancellationToken);

        if (product is null)
        {
            return ApiResponse<InventoryItemResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Product not found",
                [new ApiError("ProductNotFound", "No product found for this id in the selected store", nameof(productId))]);
        }

        if (product.Store.Business.UserId != userId)
        {
            return ApiResponse<InventoryItemResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this product",
                [new ApiError("UnauthorizedProduct", "This product does not belong to your business", nameof(productId))]);
        }

        // Validate LocationStoreId
        var locationStore = await unitOfWork.Query<Store>()
            .Include(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == request.LocationStoreId, cancellationToken);

        if (locationStore is null || locationStore.Business.UserId != userId)
        {
            return ApiResponse<InventoryItemResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "Location store not found or does not belong to your business",
                [new ApiError("UnauthorizedStore", "The location store does not belong to your business", nameof(request.LocationStoreId))]);
        }

        var now = DateTime.UtcNow;

        if (request.Type == StockAdjustmentType.StockIn)
        {
            if (request.PurchaseCostPerUnit.HasValue)
            {
                var newCost = request.PurchaseCostPerUnit.Value;
                var existingCost = decimal.TryParse(product.CostPrice, out var parsed) ? parsed : newCost;
                var existingQty = product.Quantity;

                var weightedAverage = existingQty > 0
                    ? (existingQty * existingCost + request.Quantity * newCost) / (existingQty + request.Quantity)
                    : newCost;

                product.CostPrice = Math.Round(weightedAverage, 2).ToString("F2");
            }

            product.Quantity += request.Quantity;
        }
        else
        {
            if (product.Quantity < request.Quantity)
            {
                return ApiResponse<InventoryItemResponse>.Failed(
                    StatusCodes.Status409Conflict,
                    "Insufficient stock",
                    [new ApiError("InsufficientStock", $"Cannot remove {request.Quantity} units. Available stock: {product.Quantity}", nameof(request.Quantity))]);
            }

            product.Quantity -= request.Quantity;
        }

        product.DateUpdated = now;
        product.UpdatedBy = SystemActor;

        unitOfWork.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new InventoryItemResponse
        {
            ProductId = product.Id,
            StoreId = product.StoreId,
            Name = product.Name,
            Sku = product.Sku,
            Quantity = product.Quantity,
            ReorderThreshold = product.ReorderThreshold,
            IsLowStock = product.ReorderThreshold.HasValue && product.Quantity <= product.ReorderThreshold.Value,
            IsActive = product.IsActive,
            ProductCategory = product.ProductCategory.Name,
            CostPrice = product.CostPrice
        };

        return ApiResponse<InventoryItemResponse>.Ok(response, $"Stock {(request.Type == StockAdjustmentType.StockIn ? "added" : "removed")} successfully");
    }

    private static decimal CalculatePercentageIncrease(long currentValue, long previousValue)
    {
        if (previousValue == 0)
        {
            return currentValue > 0 ? 100 : 0;
        }

        return Math.Round(((decimal)(currentValue - previousValue) / previousValue) * 100, 2);
    }

    private static decimal CalculatePercentageIncrease(int currentValue, int previousValue)
    {
        if (previousValue == 0)
        {
            return currentValue > 0 ? 100 : 0;
        }

        return Math.Round(((decimal)(currentValue - previousValue) / previousValue) * 100, 2);
    }
}
