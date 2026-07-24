using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class MetricsService(IUnitOfWork unitOfWork) : IMetricsService
{
    private const long DefaultLowStockThreshold = 5;

    public async Task<ApiResponse<MetricsResponse>> GetAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (business is null)
        {
            return ApiResponse<MetricsResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found for this user",
                [new ApiError("BusinessNotFound", "Create a business before accessing metrics", nameof(userId))]);
        }

        // Resolve store IDs in scope
        List<long> storeIds;
        if (request.StoreId.HasValue)
        {
            var storeExists = await unitOfWork.Query<Store>()
                .AnyAsync(x => x.Id == request.StoreId.Value && x.BusinessId == business.Id, cancellationToken);

            if (!storeExists)
            {
                return ApiResponse<MetricsResponse>.Failed(
                    StatusCodes.Status404NotFound,
                    "Store not found or does not belong to your business",
                    [new ApiError("StoreNotFound", "No store found for the provided id", nameof(request.StoreId))]);
            }

            storeIds = [request.StoreId.Value];
        }
        else
        {
            storeIds = await unitOfWork.Query<Store>()
                .Where(x => x.BusinessId == business.Id)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        // Resolve date range
        var to = (request.To ?? DateTime.UtcNow).ToUniversalTime();
        var from = (request.From ?? to.AddDays(-30)).ToUniversalTime();
        var periodLength = to - from;
        var prevTo = from;
        var prevFrom = from - periodLength;

        // ── Products Sold ──────────────────────────────────────────────
        var currentSold = await unitOfWork.Query<SaleItem>()
            .Where(si =>
                storeIds.Contains(si.Sale.StoreId) &&
                si.Sale.DateCreated >= from && si.Sale.DateCreated < to &&
                !si.Sale.IsDeleted)
            .SumAsync(si => (long?)si.Quantity, cancellationToken) ?? 0;

        var previousSold = await unitOfWork.Query<SaleItem>()
            .Where(si =>
                storeIds.Contains(si.Sale.StoreId) &&
                si.Sale.DateCreated >= prevFrom && si.Sale.DateCreated < prevTo &&
                !si.Sale.IsDeleted)
            .SumAsync(si => (long?)si.Quantity, cancellationToken) ?? 0;

        // ── New Customers ──────────────────────────────────────────────
        var currentCustomers = await unitOfWork.Query<Customer>()
            .CountAsync(c =>
                storeIds.Contains(c.StoreId) &&
                c.DateCreated >= from && c.DateCreated < to &&
                !c.IsDeleted, cancellationToken);

        var previousCustomers = await unitOfWork.Query<Customer>()
            .CountAsync(c =>
                storeIds.Contains(c.StoreId) &&
                c.DateCreated >= prevFrom && c.DateCreated < prevTo &&
                !c.IsDeleted, cancellationToken);

        // ── Low Stock (snapshot — not time-based) ──────────────────────
        // Current: products currently low in stock
        var currentLowStock = await unitOfWork.Query<Product>()
            .CountAsync(p =>
                storeIds.Contains(p.StoreId) &&
                !p.IsDeleted &&
                p.Quantity > 0 &&
                p.Quantity <= (p.ReorderThreshold ?? DefaultLowStockThreshold), cancellationToken);

        // Previous: products that were low stock during the previous period
        // Approximated via products created before prevTo with quantity <= threshold
        var previousLowStock = await unitOfWork.Query<Product>()
            .CountAsync(p =>
                storeIds.Contains(p.StoreId) &&
                !p.IsDeleted &&
                p.DateCreated < prevTo &&
                p.Quantity > 0 &&
                p.Quantity <= (p.ReorderThreshold ?? DefaultLowStockThreshold), cancellationToken);

        // ── Out of Stock (snapshot) ────────────────────────────────────
        var currentOutOfStock = await unitOfWork.Query<Product>()
            .CountAsync(p =>
                storeIds.Contains(p.StoreId) &&
                !p.IsDeleted &&
                p.Quantity == 0, cancellationToken);

        var previousOutOfStock = await unitOfWork.Query<Product>()
            .CountAsync(p =>
                storeIds.Contains(p.StoreId) &&
                !p.IsDeleted &&
                p.DateCreated < prevTo &&
                p.Quantity == 0, cancellationToken);

        return ApiResponse<MetricsResponse>.Ok(new MetricsResponse
        {
            CurrentPeriod = new MetricPeriod { From = from, To = to },
            PreviousPeriod = new MetricPeriod { From = prevFrom, To = prevTo },
            ProductsSold = BuildMetric(currentSold, previousSold),
            NewCustomers = BuildMetric(currentCustomers, previousCustomers),
            LowStock = BuildMetric(currentLowStock, previousLowStock),
            OutOfStock = BuildMetric(currentOutOfStock, previousOutOfStock)
        });
    }

    private static MetricItem BuildMetric(long current, long previous)
    {
        var change = current - previous;
        decimal? pct = previous > 0
            ? Math.Round((decimal)change / previous * 100, 2)
            : current > 0 ? 100m : null;

        return new MetricItem
        {
            Current = current,
            Previous = previous,
            Change = change,
            PercentageChange = pct == null ? 0 : pct
        };
    }

    public async Task<ApiResponse<SalesSummaryResponse>> GetSalesSummaryAsync(SalesSummaryRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (business is null)
        {
            return ApiResponse<SalesSummaryResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found for this user",
                [new ApiError("BusinessNotFound", "Create a business before accessing metrics", nameof(userId))]);
        }

        // Resolve store IDs in scope
        List<long> storeIds;
        if (request.StoreId.HasValue)
        {
            var storeExists = await unitOfWork.Query<Store>()
                .AnyAsync(x => x.Id == request.StoreId.Value && x.BusinessId == business.Id, cancellationToken);

            if (!storeExists)
            {
                return ApiResponse<SalesSummaryResponse>.Failed(
                    StatusCodes.Status404NotFound,
                    "Store not found or does not belong to your business",
                    [new ApiError("StoreNotFound", "No store found for the provided id", nameof(request.StoreId))]);
            }

            storeIds = [request.StoreId.Value];
        }
        else
        {
            storeIds = await unitOfWork.Query<Store>()
                .Where(x => x.BusinessId == business.Id)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        // Resolve date range — year filter takes precedence
        DateTime from, to;
        if (request.Year.HasValue)
        {
            from = new DateTime(request.Year.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            to = new DateTime(request.Year.Value, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        }
        else
        {
            to = (request.To ?? DateTime.UtcNow).ToUniversalTime();
            from = (request.From ?? to.AddDays(-30)).ToUniversalTime();
        }

        var sales = await unitOfWork.Query<Sale>()
            .Where(s =>
                storeIds.Contains(s.StoreId) &&
                s.DateCreated >= from && s.DateCreated <= to &&
                !s.IsDeleted)
            .Select(s => new
            {
                s.TotalAmount,
                s.AmountPaid,
                s.BalanceRemaining,
                s.DateCreated.Year,
                s.DateCreated.Month
            })
            .ToListAsync(cancellationToken);

        var monthly = sales
            .GroupBy(s => new { s.Year, s.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new SalesSummaryMonthBreakdown
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM"),
                TotalSales = g.Sum(s => s.TotalAmount),
                TotalSettled = g.Sum(s => s.AmountPaid),
                TotalOwed = g.Sum(s => s.BalanceRemaining),
                TotalTransactions = g.Count()
            })
            .ToList();

        return ApiResponse<SalesSummaryResponse>.Ok(new SalesSummaryResponse
        {
            From = from,
            To = to,
            TotalSales = sales.Sum(s => s.TotalAmount),
            TotalSettled = sales.Sum(s => s.AmountPaid),
            TotalOwed = sales.Sum(s => s.BalanceRemaining),
            TotalTransactions = sales.Count,
            MonthlyBreakdown = monthly
        });
    }

    public async Task<ApiResponse<SalesGraphResponse>> GetSalesGraphAsync(SalesSummaryRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (business is null)
        {
            return ApiResponse<SalesGraphResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found for this user",
                [new ApiError("BusinessNotFound", "Create a business before accessing metrics", nameof(userId))]);
        }

        List<long> storeIds;
        if (request.StoreId.HasValue)
        {
            var storeExists = await unitOfWork.Query<Store>()
                .AnyAsync(x => x.Id == request.StoreId.Value && x.BusinessId == business.Id, cancellationToken);

            if (!storeExists)
            {
                return ApiResponse<SalesGraphResponse>.Failed(
                    StatusCodes.Status404NotFound,
                    "Store not found or does not belong to your business",
                    [new ApiError("StoreNotFound", "No store found for the provided id", nameof(request.StoreId))]);
            }

            storeIds = [request.StoreId.Value];
        }
        else
        {
            storeIds = await unitOfWork.Query<Store>()
                .Where(x => x.BusinessId == business.Id)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        DateTime from, to;
        if (request.Year.HasValue)
        {
            from = new DateTime(request.Year.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            to = new DateTime(request.Year.Value, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        }
        else
        {
            to = (request.To ?? DateTime.UtcNow).ToUniversalTime();
            from = (request.From ?? to.AddDays(-30)).ToUniversalTime();
        }

        var isDaily = (to - from).TotalDays <= 90;
        var granularity = isDaily ? "daily" : "monthly";

        var sales = await unitOfWork.Query<Sale>()
            .Where(s =>
                storeIds.Contains(s.StoreId) &&
                s.DateCreated >= from && s.DateCreated <= to &&
                !s.IsDeleted)
            .Select(s => new
            {
                s.TotalAmount,
                s.AmountPaid,
                s.DateCreated.Year,
                s.DateCreated.Month,
                s.DateCreated.Day
            })
            .ToListAsync(cancellationToken);

        List<SalesGraphPoint> points;

        if (isDaily)
        {
            // Fill every day in the range so the graph has no gaps
            var lookup = sales
                .GroupBy(s => new DateTime(s.Year, s.Month, s.Day))
                .ToDictionary(g => g.Key, g => (Sales: g.Sum(x => x.TotalAmount), Settled: g.Sum(x => x.AmountPaid)));

            points = [];
            for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
            {
                lookup.TryGetValue(date, out var vals);
                points.Add(new SalesGraphPoint
                {
                    Label = date.ToString("yyyy-MM-dd"),
                    TotalSales = vals.Sales,
                    TotalSettled = vals.Settled
                });
            }
        }
        else
        {
            // Fill every month in the range
            var lookup = sales
                .GroupBy(s => new { s.Year, s.Month })
                .ToDictionary(g => g.Key, g => (Sales: g.Sum(x => x.TotalAmount), Settled: g.Sum(x => x.AmountPaid)));

            points = [];
            var cursor = new DateTime(from.Year, from.Month, 1);
            var end = new DateTime(to.Year, to.Month, 1);
            while (cursor <= end)
            {
                var key = new { cursor.Year, cursor.Month };
                lookup.TryGetValue(key, out var vals);
                points.Add(new SalesGraphPoint
                {
                    Label = cursor.ToString("yyyy-MM"),
                    TotalSales = vals.Sales,
                    TotalSettled = vals.Settled
                });
                cursor = cursor.AddMonths(1);
            }
        }

        return ApiResponse<SalesGraphResponse>.Ok(new SalesGraphResponse
        {
            From = from,
            To = to,
            Granularity = granularity,
            Points = points
        });
    }

    public async Task<ApiResponse<PagedResponse<SaleResponse>>> GetRecentTransactionsAsync(
        SalesSummaryRequest request, PagedQueryRequest paging, long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (business is null)
        {
            return ApiResponse<PagedResponse<SaleResponse>>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found for this user",
                [new ApiError("BusinessNotFound", "Create a business before accessing metrics", nameof(userId))]);
        }

        List<long> storeIds;
        if (request.StoreId.HasValue)
        {
            var storeExists = await unitOfWork.Query<Store>()
                .AnyAsync(x => x.Id == request.StoreId.Value && x.BusinessId == business.Id, cancellationToken);

            if (!storeExists)
            {
                return ApiResponse<PagedResponse<SaleResponse>>.Failed(
                    StatusCodes.Status404NotFound,
                    "Store not found or does not belong to your business",
                    [new ApiError("StoreNotFound", "No store found for the provided id", nameof(request.StoreId))]);
            }

            storeIds = [request.StoreId.Value];
        }
        else
        {
            storeIds = await unitOfWork.Query<Store>()
                .Where(x => x.BusinessId == business.Id)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        DateTime from, to;
        if (request.Year.HasValue)
        {
            from = new DateTime(request.Year.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            to = new DateTime(request.Year.Value, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        }
        else
        {
            to = (request.To ?? DateTime.UtcNow).ToUniversalTime();
            from = (request.From ?? to.AddDays(-30)).ToUniversalTime();
        }

        var pageNumber = Math.Max(1, paging.PageNumber);
        var pageSize = Math.Clamp(paging.PageSize, 1, 100);

        var query = unitOfWork.Query<Sale>()
            .Include(s => s.Customer)
            .Include(s => s.SaleItems).ThenInclude(si => si.Product)
            .Where(s =>
                storeIds.Contains(s.StoreId) &&
                s.DateCreated >= from && s.DateCreated <= to &&
                !s.IsDeleted)
            .OrderByDescending(s => s.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var sales = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = sales.Select(s => new SaleResponse
        {
            SaleId = s.Id,
            SaleNumber = s.SaleNumber,
            StoreId = s.StoreId,
            CustomerId = s.CustomerId,
            CustomerName = s.Customer?.FullName,
            SubTotal = s.SubTotal,
            DiscountType = s.DiscountType,
            DiscountValue = s.DiscountValue,
            DiscountAmount = s.DiscountAmount,
            TaxAmount = s.TaxAmount,
            TotalAmount = s.TotalAmount,
            AmountPaid = s.AmountPaid,
            BalanceRemaining = s.BalanceRemaining,
            BalanceDueDate = s.BalanceDueDate,
            ChangeAmount = s.ChangeAmount,
            PaymentMethod = s.PaymentMethod,
            Status = s.Status,
            Notes = s.Notes,
            SaleDate = s.DateCreated,
            Items = s.SaleItems.Select(si => new SaleItemResponse
            {
                SaleItemId = si.Id,
                ProductId = si.ProductId,
                ProductName = si.Product.Name,
                Quantity = si.Quantity,
                UnitPrice = si.UnitPrice,
                LineTotal = si.LineTotal
            }).ToList()
        }).ToList();

        return ApiResponse<PagedResponse<SaleResponse>>.Ok(new PagedResponse<SaleResponse>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items
        });
    }

    public async Task<ApiResponse<SaleResponse>> GetTransactionByIdAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (business is null)
            return ApiResponse<SaleResponse>.Failed(StatusCodes.Status404NotFound,
                "Business not found for this user",
                [new ApiError("BusinessNotFound", "Create a business before accessing transactions", nameof(userId))]);

        var storeIds = await unitOfWork.Query<Store>()
            .Where(x => x.BusinessId == business.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var sale = await unitOfWork.Query<Sale>()
            .Include(s => s.Customer)
            .Include(s => s.SaleItems).ThenInclude(si => si.Product)
            .FirstOrDefaultAsync(s => s.Id == id && storeIds.Contains(s.StoreId) && !s.IsDeleted, cancellationToken);

        if (sale is null)
            return ApiResponse<SaleResponse>.Failed(StatusCodes.Status404NotFound,
                "Transaction not found",
                [new ApiError("NotFound", "No transaction found for this id within your business", nameof(id))]);

        return ApiResponse<SaleResponse>.Ok(new SaleResponse
        {
            SaleId = sale.Id,
            SaleNumber = sale.SaleNumber,
            StoreId = sale.StoreId,
            CustomerId = sale.CustomerId,
            CustomerName = sale.Customer?.FullName,
            SubTotal = sale.SubTotal,
            DiscountType = sale.DiscountType,
            DiscountValue = sale.DiscountValue,
            DiscountAmount = sale.DiscountAmount,
            TaxAmount = sale.TaxAmount,
            TotalAmount = sale.TotalAmount,
            AmountPaid = sale.AmountPaid,
            BalanceRemaining = sale.BalanceRemaining,
            BalanceDueDate = sale.BalanceDueDate,
            ChangeAmount = sale.ChangeAmount,
            PaymentMethod = sale.PaymentMethod,
            Status = sale.Status,
            Notes = sale.Notes,
            SaleDate = sale.DateCreated,
            Items = sale.SaleItems.Select(si => new SaleItemResponse
            {
                SaleItemId = si.Id,
                ProductId = si.ProductId,
                ProductName = si.Product.Name,
                Quantity = si.Quantity,
                UnitPrice = si.UnitPrice,
                LineTotal = si.LineTotal
            }).ToList()
        });
    }

    public async Task<ApiResponse<ProductMetricsResponse>> GetProductMetricsAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (business is null)
        {
            return ApiResponse<ProductMetricsResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found for this user",
                [new ApiError("BusinessNotFound", "Create a business before accessing metrics", nameof(userId))]);
        }

        List<long> storeIds;
        if (request.StoreId.HasValue)
        {
            var storeExists = await unitOfWork.Query<Store>()
                .AnyAsync(x => x.Id == request.StoreId.Value && x.BusinessId == business.Id, cancellationToken);

            if (!storeExists)
            {
                return ApiResponse<ProductMetricsResponse>.Failed(
                    StatusCodes.Status404NotFound,
                    "Store not found or does not belong to your business",
                    [new ApiError("StoreNotFound", "No store found for the provided id", nameof(request.StoreId))]);
            }

            storeIds = [request.StoreId.Value];
        }
        else
        {
            storeIds = await unitOfWork.Query<Store>()
                .Where(x => x.BusinessId == business.Id)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        var to = (request.To ?? DateTime.UtcNow).ToUniversalTime();
        var from = (request.From ?? to.AddDays(-30)).ToUniversalTime();
        var periodLength = to - from;
        var prevTo = from;
        var prevFrom = from - periodLength;

        // ── Total Products ─────────────────────────────────────────────
        var currentProducts = await unitOfWork.Query<Product>()
            .CountAsync(p => storeIds.Contains(p.StoreId) && !p.IsDeleted, cancellationToken);

        var previousProducts = await unitOfWork.Query<Product>()
            .CountAsync(p => storeIds.Contains(p.StoreId) && !p.IsDeleted && p.DateCreated < prevTo, cancellationToken);

        // ── Total Categories ───────────────────────────────────────────
        var totalCategories = await unitOfWork.Query<Product>()
            .Where(p => storeIds.Contains(p.StoreId) && !p.IsDeleted)
            .Select(p => p.ProductCategoryId)
            .Distinct()
            .CountAsync(cancellationToken);

        // ── Average Price ──────────────────────────────────────────────
        var prices = await unitOfWork.Query<Product>()
            .Where(p => storeIds.Contains(p.StoreId) && !p.IsDeleted)
            .Select(p => p.Price)
            .ToListAsync(cancellationToken);

        var averagePrice = prices.Count > 0
            ? prices
                .Select(p => decimal.TryParse(p, out var v) ? v : 0m)
                .Average()
            : 0m;

        // ── Most Sold (top 10 by units in current period) ──────────────
        var mostSold = await unitOfWork.Query<SaleItem>()
            .Where(si =>
                storeIds.Contains(si.Sale.StoreId) &&
                si.Sale.DateCreated >= from && si.Sale.DateCreated < to &&
                !si.Sale.IsDeleted)
            .GroupBy(si => new { si.ProductId, si.Product.Name, si.Product.Sku })
            .Select(g => new MostSoldProductResponse
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                Sku = g.Key.Sku,
                UnitsSold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(x => x.UnitsSold)
            .Take(10)
            .ToListAsync(cancellationToken);

        return ApiResponse<ProductMetricsResponse>.Ok(new ProductMetricsResponse
        {
            CurrentPeriod = new MetricPeriod { From = from, To = to },
            PreviousPeriod = new MetricPeriod { From = prevFrom, To = prevTo },
            TotalProducts = BuildMetric(currentProducts, previousProducts),
            TotalCategories = totalCategories,
            AveragePrice = Math.Round(averagePrice, 2),
            MostSold = mostSold
        });
    }
    public async Task<ApiResponse<InventoryMetricsResponse>> GetInventoryMetricsAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (business is null)
        {
            return ApiResponse<InventoryMetricsResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found for this user",
                [new ApiError("BusinessNotFound", "Create a business before accessing metrics", nameof(userId))]);
        }

        // Resolve stores in scope
        var storesQuery = unitOfWork.Query<Store>()
            .Where(x => x.BusinessId == business.Id);

        if (request.StoreId.HasValue)
        {
            var storeExists = await storesQuery
                .AnyAsync(x => x.Id == request.StoreId.Value, cancellationToken);

            if (!storeExists)
            {
                return ApiResponse<InventoryMetricsResponse>.Failed(
                    StatusCodes.Status404NotFound,
                    "Store not found or does not belong to your business",
                    [new ApiError("StoreNotFound", "No store found for the provided id", nameof(request.StoreId))]);
            }

            storesQuery = storesQuery.Where(x => x.Id == request.StoreId.Value);
        }

        var stores = await storesQuery.ToListAsync(cancellationToken);
        var storeIds = stores.Select(s => s.Id).ToList();

        var to = (request.To ?? DateTime.UtcNow).ToUniversalTime();
        var from = (request.From ?? to.AddDays(-30)).ToUniversalTime();
        var periodLength = to - from;
        var prevTo = from;
        var prevFrom = from - periodLength;

        // All business store IDs — used for business-wide stock value
        var allBusinessStoreIds = await unitOfWork.Query<Store>()
            .Where(x => x.BusinessId == business.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        // Load all active products in scope (current snapshot) — store-filtered for low/out-of-stock
        var products = await unitOfWork.Query<Product>()
            .Where(p => storeIds.Contains(p.StoreId) && !p.IsDeleted)
            .Select(p => new
            {
                p.StoreId,
                p.Quantity,
                p.CostPrice,
                p.ReorderThreshold,
                p.DateCreated
            })
            .ToListAsync(cancellationToken);

        // Load ALL products across the entire business for stock value calculation
        var allBusinessProducts = await unitOfWork.Query<Product>()
            .Where(p => allBusinessStoreIds.Contains(p.StoreId) && !p.IsDeleted)
            .Select(p => new
            {
                p.Quantity,
                p.Price,
                p.DateCreated
            })
            .ToListAsync(cancellationToken);

        // ── Total Stock Value ──────────────────────────────────────────
        static decimal StockValue(decimal qty, string? costPriceStr)
        {
            if (!decimal.TryParse(costPriceStr, out var cost)) return 0m;
            return qty * cost;
        }

        var currentStockValue = allBusinessProducts
            .Sum(p => StockValue(p.Quantity, p.Price));

        var prevBusinessProducts = allBusinessProducts.Where(p => p.DateCreated < prevTo).ToList();
        var previousStockValue = prevBusinessProducts
            .Sum(p => StockValue(p.Quantity, p.Price));

        var stockValueChange = currentStockValue - previousStockValue;
        decimal? stockValuePct = previousStockValue > 0
            ? Math.Round(stockValueChange / previousStockValue * 100, 2)
            : currentStockValue > 0 ? 100m : null;

        // prevProducts — store-scoped, used for Low/Out-of-stock previous period counts
        var prevProducts = products.Where(p => p.DateCreated < prevTo).ToList();

        // ── Low Stock ──────────────────────────────────────────────────
        var currentLow = products.Count(p =>
            p.Quantity > 0 && p.Quantity <= (p.ReorderThreshold ?? DefaultLowStockThreshold));
        var previousLow = prevProducts.Count(p =>
            p.Quantity > 0 && p.Quantity <= (p.ReorderThreshold ?? DefaultLowStockThreshold));

        // ── Out Of Stock ───────────────────────────────────────────────
        var currentOut = products.Count(p => p.Quantity == 0);
        var previousOut = prevProducts.Count(p => p.Quantity == 0);

        // ── Stock Locations ────────────────────────────────────────────
        var productsByStore = products.GroupBy(p => p.StoreId).ToDictionary(g => g.Key, g => g.ToList());

        var stockLocations = stores.Select(store =>
        {
            var storeProducts = productsByStore.TryGetValue(store.Id, out var sp) ? sp : [];
            return new StockLocationResponse
            {
                StoreId = store.Id,
                StoreName = store.Name,
                TotalProducts = storeProducts.Count,
                TotalUnits = storeProducts.Sum(p => p.Quantity),
                TotalStockValue = Math.Round(storeProducts.Sum(p => StockValue(p.Quantity, p.CostPrice)), 2),
                LowStockCount = storeProducts.Count(p =>
                    p.Quantity > 0 && p.Quantity <= (p.ReorderThreshold ?? DefaultLowStockThreshold)),
                OutOfStockCount = storeProducts.Count(p => p.Quantity == 0)
            };
        }).ToList();

        return ApiResponse<InventoryMetricsResponse>.Ok(new InventoryMetricsResponse
        {
            CurrentPeriod = new MetricPeriod { From = from, To = to },
            PreviousPeriod = new MetricPeriod { From = prevFrom, To = prevTo },
            TotalStockValue = new MetricItemDecimal
            {
                Current = Math.Round(currentStockValue, 2),
                Previous = Math.Round(previousStockValue, 2),
                Change = Math.Round(stockValueChange, 2),
                PercentageChange = stockValuePct
            },
            LowStockItems = BuildMetric(currentLow, previousLow),
            OutOfStockItems = BuildMetric(currentOut, previousOut),
            StockLocations = stockLocations
        });
    }

    public async Task<ApiResponse<CustomerMetricsResponse>> GetCustomerMetricsAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (business is null)
        {
            return ApiResponse<CustomerMetricsResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found for this user",
                [new ApiError("BusinessNotFound", "Create a business before accessing metrics", nameof(userId))]);
        }

        List<long> storeIds;
        if (request.StoreId.HasValue)
        {
            var storeExists = await unitOfWork.Query<Store>()
                .AnyAsync(x => x.Id == request.StoreId.Value && x.BusinessId == business.Id, cancellationToken);

            if (!storeExists)
            {
                return ApiResponse<CustomerMetricsResponse>.Failed(
                    StatusCodes.Status404NotFound,
                    "Store not found or does not belong to your business",
                    [new ApiError("StoreNotFound", "No store found for the provided id", nameof(request.StoreId))]);
            }

            storeIds = [request.StoreId.Value];
        }
        else
        {
            storeIds = await unitOfWork.Query<Store>()
                .Where(x => x.BusinessId == business.Id)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        var to = (request.To ?? DateTime.UtcNow).ToUniversalTime();
        var from = (request.From ?? to.AddDays(-30)).ToUniversalTime();
        var periodLength = to - from;
        var prevTo = from;
        var prevFrom = from - periodLength;

        // ── Total Customers ────────────────────────────────────────────
        var currentTotal = await unitOfWork.Query<Customer>()
            .CountAsync(c => storeIds.Contains(c.StoreId) && !c.IsDeleted, cancellationToken);

        var previousTotal = await unitOfWork.Query<Customer>()
            .CountAsync(c => storeIds.Contains(c.StoreId) && !c.IsDeleted && c.DateCreated < prevTo, cancellationToken);

        // ── New Customers ──────────────────────────────────────────────
        var currentNew = await unitOfWork.Query<Customer>()
            .CountAsync(c => storeIds.Contains(c.StoreId) && !c.IsDeleted &&
                             c.DateCreated >= from && c.DateCreated < to, cancellationToken);

        var previousNew = await unitOfWork.Query<Customer>()
            .CountAsync(c => storeIds.Contains(c.StoreId) && !c.IsDeleted &&
                             c.DateCreated >= prevFrom && c.DateCreated < prevTo, cancellationToken);

        // ── Outstanding Balance ────────────────────────────────────────
        var outstandingBalance = await unitOfWork.Query<Sale>()
            .Where(s => storeIds.Contains(s.StoreId) && !s.IsDeleted && s.BalanceRemaining > 0)
            .SumAsync(s => (decimal?)s.BalanceRemaining, cancellationToken) ?? 0m;

        // ── Repeat Customers ───────────────────────────────────────────
        // A repeat customer is one with more than 1 sale in the current period
        var salesInPeriod = await unitOfWork.Query<Sale>()
            .Where(s =>
                storeIds.Contains(s.StoreId) &&
                !s.IsDeleted &&
                s.CustomerId.HasValue &&
                s.DateCreated >= from && s.DateCreated < to)
            .GroupBy(s => s.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var totalWithPurchases = salesInPeriod.Count;
        var repeatCount = salesInPeriod.Count(g => g.Count > 1);
        var repeatPct = totalWithPurchases > 0
            ? Math.Round((decimal)repeatCount / totalWithPurchases * 100, 2)
            : 0m;

        return ApiResponse<CustomerMetricsResponse>.Ok(new CustomerMetricsResponse
        {
            CurrentPeriod = new MetricPeriod { From = from, To = to },
            PreviousPeriod = new MetricPeriod { From = prevFrom, To = prevTo },
            TotalCustomers = BuildMetric(currentTotal, previousTotal),
            NewCustomers = BuildMetric(currentNew, previousNew),
            OutstandingBalance = Math.Round(outstandingBalance, 2),
            RepeatCustomerCount = repeatCount,
            RepeatCustomerPercentage = repeatPct
        });
    }

    // ── Helper: resolve business + storeIds ────────────────────────────────────
    private async Task<(Business? business, List<long> storeIds, bool failed)> ResolveBusinessAndStoresAsync<T>(
        MetricsRequest request, long userId,
        Func<string, string, ApiError[], ApiResponse<T>> failFn,
        CancellationToken cancellationToken)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (business is null)
            return (null, [], true);

        List<long> storeIds;
        if (request.StoreId.HasValue)
        {
            var storeExists = await unitOfWork.Query<Store>()
                .AnyAsync(x => x.Id == request.StoreId.Value && x.BusinessId == business.Id, cancellationToken);
            if (!storeExists)
                return (null, [], true);
            storeIds = [request.StoreId.Value];
        }
        else
        {
            storeIds = await unitOfWork.Query<Store>()
                .Where(x => x.BusinessId == business.Id)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        return (business, storeIds, false);
    }

    // ── Sales by location ──────────────────────────────────────────────────────
    public async Task<ApiResponse<SalesByLocationResponse>> GetSalesByLocationAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (business is null)
            return ApiResponse<SalesByLocationResponse>.Failed(StatusCodes.Status404NotFound,
                "Business not found for this user",
                [new ApiError("BusinessNotFound", "Create a business before accessing metrics", nameof(userId))]);

        var to = (request.To ?? DateTime.UtcNow).ToUniversalTime();
        var from = (request.From ?? to.AddDays(-30)).ToUniversalTime();

        var stores = await unitOfWork.Query<Store>()
            .Where(x => x.BusinessId == business.Id && (!request.StoreId.HasValue || x.Id == request.StoreId.Value))
            .Select(x => new { x.Id, x.Name })
            .ToListAsync(cancellationToken);

        var storeIds = stores.Select(x => x.Id).ToList();

        var rows = await unitOfWork.Query<Sale>()
            .Where(s => storeIds.Contains(s.StoreId) && !s.IsDeleted && s.DateCreated >= from && s.DateCreated < to)
            .GroupBy(s => s.StoreId)
            .Select(g => new { StoreId = g.Key, Revenue = g.Sum(s => s.TotalAmount), Count = g.Count() })
            .ToListAsync(cancellationToken);

        var totalRevenue = rows.Sum(r => r.Revenue);

        var locations = stores.Select(st =>
        {
            var row = rows.FirstOrDefault(r => r.StoreId == st.Id);
            var rev = row?.Revenue ?? 0m;
            return new LocationSalesRow
            {
                StoreId = st.Id,
                StoreName = st.Name,
                TransactionCount = row?.Count ?? 0,
                Revenue = Math.Round(rev, 2),
                RevenueShare = totalRevenue > 0 ? Math.Round(rev / totalRevenue * 100, 2) : 0m
            };
        }).OrderByDescending(x => x.Revenue).ToList();

        return ApiResponse<SalesByLocationResponse>.Ok(new SalesByLocationResponse
        {
            CurrentPeriod = new MetricPeriod { From = from, To = to },
            Locations = locations
        });
    }

    // ── Sales by category ──────────────────────────────────────────────────────
    public async Task<ApiResponse<SalesByCategoryResponse>> GetSalesByCategoryAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (business is null)
            return ApiResponse<SalesByCategoryResponse>.Failed(StatusCodes.Status404NotFound,
                "Business not found for this user",
                [new ApiError("BusinessNotFound", "Create a business before accessing metrics", nameof(userId))]);

        var to = (request.To ?? DateTime.UtcNow).ToUniversalTime();
        var from = (request.From ?? to.AddDays(-30)).ToUniversalTime();

        var storeIds = await ResolveStoreIdsAsync(business.Id, request.StoreId, cancellationToken);

        var rows = await unitOfWork.Query<SaleItem>()
            .Where(si => !si.IsDeleted && si.Sale != null && !si.Sale.IsDeleted &&
                         storeIds.Contains(si.Sale.StoreId) &&
                         si.Sale.DateCreated >= from && si.Sale.DateCreated < to)
            .Join(unitOfWork.Query<Product>().Where(p => !p.IsDeleted),
                  si => si.ProductId, p => p.Id,
                  (si, p) => new { p.ProductCategoryId, si.Quantity, si.LineTotal })
            .GroupBy(x => x.ProductCategoryId)
            .Select(g => new { CategoryId = g.Key, Units = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.LineTotal) })
            .ToListAsync(cancellationToken);

        var categoryIds = rows.Select(r => r.CategoryId).ToList();
        var categories = await unitOfWork.Query<ProductCategory>()
            .Where(c => categoryIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(cancellationToken);

        var totalRevenue = rows.Sum(r => r.Revenue);

        var result = rows
            .Join(categories, r => r.CategoryId, c => c.Id, (r, c) => new CategorySalesRow
            {
                CategoryId = c.Id,
                CategoryName = c.Name,
                UnitsSold = r.Units,
                Revenue = Math.Round(r.Revenue, 2),
                RevenueShare = totalRevenue > 0 ? Math.Round(r.Revenue / totalRevenue * 100, 2) : 0m
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        return ApiResponse<SalesByCategoryResponse>.Ok(new SalesByCategoryResponse
        {
            CurrentPeriod = new MetricPeriod { From = from, To = to },
            Categories = result
        });
    }

    // ── Payment method breakdown ───────────────────────────────────────────────
    public async Task<ApiResponse<PaymentMethodBreakdownResponse>> GetPaymentMethodBreakdownAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (business is null)
            return ApiResponse<PaymentMethodBreakdownResponse>.Failed(StatusCodes.Status404NotFound,
                "Business not found for this user",
                [new ApiError("BusinessNotFound", "Create a business before accessing metrics", nameof(userId))]);

        var to = (request.To ?? DateTime.UtcNow).ToUniversalTime();
        var from = (request.From ?? to.AddDays(-30)).ToUniversalTime();

        var storeIds = await ResolveStoreIdsAsync(business.Id, request.StoreId, cancellationToken);

        var rows = await unitOfWork.Query<Sale>()
            .Where(s => storeIds.Contains(s.StoreId) && !s.IsDeleted && s.DateCreated >= from && s.DateCreated < to)
            .GroupBy(s => s.PaymentMethod)
            .Select(g => new { Method = g.Key.ToString(), Revenue = g.Sum(s => s.TotalAmount), Count = g.Count() })
            .ToListAsync(cancellationToken);

        var totalRevenue = rows.Sum(r => r.Revenue);

        var result = rows
            .Select(r => new PaymentMethodRow
            {
                Method = r.Method,
                TransactionCount = r.Count,
                Revenue = Math.Round(r.Revenue, 2),
                RevenueShare = totalRevenue > 0 ? Math.Round(r.Revenue / totalRevenue * 100, 2) : 0m
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        return ApiResponse<PaymentMethodBreakdownResponse>.Ok(new PaymentMethodBreakdownResponse
        {
            CurrentPeriod = new MetricPeriod { From = from, To = to },
            Methods = result
        });
    }

    // ── Top customers ──────────────────────────────────────────────────────────
    public async Task<ApiResponse<TopCustomersResponse>> GetTopCustomersAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (business is null)
            return ApiResponse<TopCustomersResponse>.Failed(StatusCodes.Status404NotFound,
                "Business not found for this user",
                [new ApiError("BusinessNotFound", "Create a business before accessing metrics", nameof(userId))]);

        var to = (request.To ?? DateTime.UtcNow).ToUniversalTime();
        var from = (request.From ?? to.AddDays(-30)).ToUniversalTime();

        var storeIds = await ResolveStoreIdsAsync(business.Id, request.StoreId, cancellationToken);

        var salesWithCustomer = await unitOfWork.Query<Sale>()
            .Where(s => storeIds.Contains(s.StoreId) && !s.IsDeleted &&
                        s.CustomerId.HasValue &&
                        s.DateCreated >= from && s.DateCreated < to)
            .GroupBy(s => s.CustomerId!.Value)
            .Select(g => new { CustomerId = g.Key, TotalSpend = g.Sum(s => s.TotalAmount), OrderCount = g.Count() })
            .ToListAsync(cancellationToken);

        var customerIds = salesWithCustomer.Select(x => x.CustomerId).ToList();
        var customers = await unitOfWork.Query<Customer>()
            .Where(c => customerIds.Contains(c.Id) && !c.IsDeleted)
            .Select(c => new { c.Id, c.FullName })
            .ToListAsync(cancellationToken);

        var joined = salesWithCustomer
            .Join(customers, s => s.CustomerId, c => c.Id, (s, c) => new TopCustomerRow
            {
                CustomerId = c.Id,
                CustomerName = c.FullName,
                OrderCount = s.OrderCount,
                TotalSpend = Math.Round(s.TotalSpend, 2)
            })
            .ToList();

        return ApiResponse<TopCustomersResponse>.Ok(new TopCustomersResponse
        {
            CurrentPeriod = new MetricPeriod { From = from, To = to },
            BySpend = joined.OrderByDescending(x => x.TotalSpend).Take(10).ToList(),
            ByOrderCount = joined.OrderByDescending(x => x.OrderCount).Take(10).ToList()
        });
    }

    // ── Customer growth time-series ────────────────────────────────────────────
    public async Task<ApiResponse<CustomerGrowthResponse>> GetCustomerGrowthAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (business is null)
            return ApiResponse<CustomerGrowthResponse>.Failed(StatusCodes.Status404NotFound,
                "Business not found for this user",
                [new ApiError("BusinessNotFound", "Create a business before accessing metrics", nameof(userId))]);

        var to = (request.To ?? DateTime.UtcNow).ToUniversalTime();
        var from = (request.From ?? to.AddDays(-30)).ToUniversalTime();

        var storeIds = await ResolveStoreIdsAsync(business.Id, request.StoreId, cancellationToken);

        var isDaily = (to - from).TotalDays <= 31;

        var customers = await unitOfWork.Query<Customer>()
            .Where(c => storeIds.Contains(c.StoreId) && !c.IsDeleted && c.DateCreated >= from && c.DateCreated < to)
            .Select(c => c.DateCreated)
            .ToListAsync(cancellationToken);

        // Total before the period start for cumulative baseline
        var baselineCount = await unitOfWork.Query<Customer>()
            .CountAsync(c => storeIds.Contains(c.StoreId) && !c.IsDeleted && c.DateCreated < from, cancellationToken);

        List<CustomerGrowthPoint> series;
        if (isDaily)
        {
            var grouped = customers
                .GroupBy(d => d.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var days = (int)(to - from).TotalDays;
            var cumulative = baselineCount;
            series = Enumerable.Range(0, days).Select(i =>
            {
                var day = from.Date.AddDays(i);
                var newCount = grouped.GetValueOrDefault(day, 0);
                cumulative += newCount;
                return new CustomerGrowthPoint
                {
                    Label = day.ToString("MMM dd"),
                    NewCustomers = newCount,
                    CumulativeTotal = cumulative
                };
            }).ToList();
        }
        else
        {
            var grouped = customers
                .GroupBy(d => new { d.Year, d.Month })
                .ToDictionary(g => g.Key, g => g.Count());

            var current = new DateTime(from.Year, from.Month, 1);
            var end = new DateTime(to.Year, to.Month, 1);
            var cumulative = baselineCount;
            series = [];
            while (current <= end)
            {
                var newCount = grouped.GetValueOrDefault(new { current.Year, current.Month }, 0);
                cumulative += newCount;
                series.Add(new CustomerGrowthPoint
                {
                    Label = current.ToString("MMM yyyy"),
                    NewCustomers = newCount,
                    CumulativeTotal = cumulative
                });
                current = current.AddMonths(1);
            }
        }

        return ApiResponse<CustomerGrowthResponse>.Ok(new CustomerGrowthResponse
        {
            CurrentPeriod = new MetricPeriod { From = from, To = to },
            Series = series
        });
    }

    // ── Staff analytics ────────────────────────────────────────────────────────
    public async Task<ApiResponse<StaffAnalyticsResponse>> GetStaffAnalyticsAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (business is null)
            return ApiResponse<StaffAnalyticsResponse>.Failed(StatusCodes.Status404NotFound,
                "Business not found for this user",
                [new ApiError("BusinessNotFound", "Create a business before accessing metrics", nameof(userId))]);

        var to = (request.To ?? DateTime.UtcNow).ToUniversalTime();
        var from = (request.From ?? to.AddDays(-30)).ToUniversalTime();

        var storeIds = await ResolveStoreIdsAsync(business.Id, request.StoreId, cancellationToken);

        var rows = await unitOfWork.Query<Sale>()
            .Where(s => storeIds.Contains(s.StoreId) && !s.IsDeleted &&
                        s.StaffId.HasValue &&
                        s.DateCreated >= from && s.DateCreated < to)
            .GroupBy(s => s.StaffId!.Value)
            .Select(g => new { StaffId = g.Key, Revenue = g.Sum(s => s.TotalAmount), Count = g.Count() })
            .ToListAsync(cancellationToken);

        var staffIds = rows.Select(r => r.StaffId).ToList();
        var staffMembers = await unitOfWork.Query<Staff>()
            .Where(s => staffIds.Contains(s.Id) && !s.IsDeleted)
            .Include(s => s.Role)
            .Select(s => new { s.Id, s.FullName, RoleName = s.Role.Name })
            .ToListAsync(cancellationToken);

        var result = rows
            .Join(staffMembers, r => r.StaffId, s => s.Id, (r, s) => new StaffAnalyticsRow
            {
                StaffId = s.Id,
                StaffName = s.FullName,
                Role = s.RoleName,
                TransactionCount = r.Count,
                Revenue = Math.Round(r.Revenue, 2),
                AverageOrderValue = r.Count > 0 ? Math.Round(r.Revenue / r.Count, 2) : 0m
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        return ApiResponse<StaffAnalyticsResponse>.Ok(new StaffAnalyticsResponse
        {
            CurrentPeriod = new MetricPeriod { From = from, To = to },
            Staff = result
        });
    }

    // ── Product performance (profitability, growth, slow-movers) ──────────────
    public async Task<ApiResponse<ProductPerformanceResponse>> GetProductPerformanceAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (business is null)
            return ApiResponse<ProductPerformanceResponse>.Failed(StatusCodes.Status404NotFound,
                "Business not found for this user",
                [new ApiError("BusinessNotFound", "Create a business before accessing metrics", nameof(userId))]);

        var to = (request.To ?? DateTime.UtcNow).ToUniversalTime();
        var from = (request.From ?? to.AddDays(-30)).ToUniversalTime();
        var periodLength = to - from;
        var prevTo = from;
        var prevFrom = from - periodLength;

        var storeIds = await ResolveStoreIdsAsync(business.Id, request.StoreId, cancellationToken);

        // Current period sales per product
        var currentSales = await unitOfWork.Query<SaleItem>()
            .Where(si => !si.IsDeleted && si.Sale != null && !si.Sale.IsDeleted &&
                         storeIds.Contains(si.Sale.StoreId) &&
                         si.Sale.DateCreated >= from && si.Sale.DateCreated < to)
            .GroupBy(si => si.ProductId)
            .Select(g => new { ProductId = g.Key, Units = g.Sum(si => si.Quantity), Revenue = g.Sum(si => si.LineTotal) })
            .ToListAsync(cancellationToken);

        // Previous period sales per product
        var previousSales = await unitOfWork.Query<SaleItem>()
            .Where(si => !si.IsDeleted && si.Sale != null && !si.Sale.IsDeleted &&
                         storeIds.Contains(si.Sale.StoreId) &&
                         si.Sale.DateCreated >= prevFrom && si.Sale.DateCreated < prevTo)
            .GroupBy(si => si.ProductId)
            .Select(g => new { ProductId = g.Key, Units = g.Sum(si => si.Quantity) })
            .ToListAsync(cancellationToken);

        var soldProductIds = currentSales.Select(x => x.ProductId).ToList();
        var allProducts = await unitOfWork.Query<Product>()
            .Where(p => storeIds.Contains(p.StoreId) && !p.IsDeleted)
            .Select(p => new { p.Id, p.Name, p.Sku, p.CostPrice, p.Quantity })
            .ToListAsync(cancellationToken);

        var productMap = allProducts.ToDictionary(p => p.Id);
        var prevMap = previousSales.ToDictionary(p => p.ProductId, p => p.Units);

        // Most sold
        var mostSold = currentSales
            .Join(allProducts, s => s.ProductId, p => p.Id, (s, p) => new MostSoldProductResponse
            {
                ProductId = p.Id,
                ProductName = p.Name,
                Sku = p.Sku,
                UnitsSold = s.Units,
                Revenue = Math.Round(s.Revenue, 2)
            })
            .OrderByDescending(x => x.UnitsSold)
            .Take(10)
            .ToList();

        // Most profitable
        var mostProfitable = currentSales
            .Where(s => productMap.ContainsKey(s.ProductId))
            .Select(s =>
            {
                var p = productMap[s.ProductId];
                var costPrice = decimal.TryParse(p.CostPrice, out var cp) ? cp : 0m;
                var cogs = costPrice * s.Units;
                var profit = s.Revenue - cogs;
                decimal? margin = s.Revenue > 0 ? Math.Round(profit / s.Revenue * 100, 2) : null;
                return new MostProfitableProductRow
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    Sku = p.Sku,
                    UnitsSold = s.Units,
                    Revenue = Math.Round(s.Revenue, 2),
                    GrossProfit = Math.Round(profit, 2),
                    ProfitMarginPct = margin
                };
            })
            .OrderByDescending(x => x.GrossProfit)
            .Take(10)
            .ToList();

        // Fastest growing
        var fastestGrowing = currentSales
            .Where(s => productMap.ContainsKey(s.ProductId))
            .Select(s =>
            {
                var prev = prevMap.GetValueOrDefault(s.ProductId, 0);
                decimal? growth = prev > 0
                    ? Math.Round((decimal)(s.Units - prev) / prev * 100, 2)
                    : s.Units > 0 ? 100m : null;
                return new FastestGrowingProductRow
                {
                    ProductId = s.ProductId,
                    ProductName = productMap[s.ProductId].Name,
                    Sku = productMap[s.ProductId].Sku,
                    CurrentUnitsSold = s.Units,
                    PreviousUnitsSold = prev,
                    GrowthPct = growth
                };
            })
            .Where(x => x.GrowthPct.HasValue)
            .OrderByDescending(x => x.GrowthPct)
            .Take(10)
            .ToList();

        // Slow moving — products with stock but 0 sales in the period
        var slowMoving = allProducts
            .Where(p => p.Quantity > 0 && !soldProductIds.Contains(p.Id))
            .Select(p =>
            {
                var costPrice = decimal.TryParse(p.CostPrice, out var cp) ? cp : (decimal?)null;
                return new SlowMovingProductRow
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    Sku = p.Sku,
                    StockOnHand = p.Quantity,
                    CostPrice = costPrice.HasValue ? Math.Round(costPrice.Value, 2) : null,
                    StockValue = costPrice.HasValue ? Math.Round(costPrice.Value * p.Quantity, 2) : null
                };
            })
            .OrderByDescending(x => x.StockValue ?? 0)
            .Take(20)
            .ToList();

        return ApiResponse<ProductPerformanceResponse>.Ok(new ProductPerformanceResponse
        {
            CurrentPeriod = new MetricPeriod { From = from, To = to },
            PreviousPeriod = new MetricPeriod { From = prevFrom, To = prevTo },
            MostSold = mostSold,
            MostProfitable = mostProfitable,
            FastestGrowing = fastestGrowing,
            SlowMoving = slowMoving
        });
    }

    // ── Financial metrics ──────────────────────────────────────────────────────
    public async Task<ApiResponse<FinancialMetricsResponse>> GetFinancialMetricsAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (business is null)
            return ApiResponse<FinancialMetricsResponse>.Failed(StatusCodes.Status404NotFound,
                "Business not found for this user",
                [new ApiError("BusinessNotFound", "Create a business before accessing metrics", nameof(userId))]);

        var to = (request.To ?? DateTime.UtcNow).ToUniversalTime();
        var from = (request.From ?? to.AddDays(-30)).ToUniversalTime();
        var periodLength = to - from;
        var prevTo = from;
        var prevFrom = from - periodLength;

        var storeIds = await ResolveStoreIdsAsync(business.Id, request.StoreId, cancellationToken);

        // Revenue current vs previous
        var currentRevenue = await unitOfWork.Query<Sale>()
            .Where(s => storeIds.Contains(s.StoreId) && !s.IsDeleted && s.DateCreated >= from && s.DateCreated < to)
            .SumAsync(s => (decimal?)s.TotalAmount, cancellationToken) ?? 0m;

        var previousRevenue = await unitOfWork.Query<Sale>()
            .Where(s => storeIds.Contains(s.StoreId) && !s.IsDeleted && s.DateCreated >= prevFrom && s.DateCreated < prevTo)
            .SumAsync(s => (decimal?)s.TotalAmount, cancellationToken) ?? 0m;

        var revenueChange = currentRevenue - previousRevenue;
        decimal? revenuePct = previousRevenue > 0
            ? Math.Round(revenueChange / previousRevenue * 100, 2)
            : currentRevenue > 0 ? 100m : null;

        // COGS — join sale items with product cost prices
        var saleItemsWithCost = await unitOfWork.Query<SaleItem>()
            .Where(si => !si.IsDeleted && si.Sale != null && !si.Sale.IsDeleted &&
                         storeIds.Contains(si.Sale.StoreId) &&
                         si.Sale.DateCreated >= from && si.Sale.DateCreated < to)
            .Join(unitOfWork.Query<Product>().Where(p => !p.IsDeleted),
                  si => si.ProductId, p => p.Id,
                  (si, p) => new
                  {
                      si.Quantity,
                      si.LineTotal,
                      p.CostPrice,
                      p.ProductCategoryId
                  })
            .ToListAsync(cancellationToken);

        var categoryIds = saleItemsWithCost.Select(x => x.ProductCategoryId).Distinct().ToList();
        var categories = await unitOfWork.Query<ProductCategory>()
            .Where(c => categoryIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        decimal totalCogs = 0m;
        var catMap = new Dictionary<long, (decimal Revenue, decimal Cogs, string Name)>();

        foreach (var item in saleItemsWithCost)
        {
            var cp = decimal.TryParse(item.CostPrice, out var cpParsed) ? cpParsed : 0m;
            var itemCogs = cp * item.Quantity;
            totalCogs += itemCogs;

            if (catMap.TryGetValue(item.ProductCategoryId, out var existing))
                catMap[item.ProductCategoryId] = (existing.Revenue + item.LineTotal, existing.Cogs + itemCogs, existing.Name);
            else
            {
                var catName = categories.TryGetValue(item.ProductCategoryId, out var cat) ? cat.Name : "Unknown";
                catMap[item.ProductCategoryId] = (item.LineTotal, itemCogs, catName);
            }
        }

        var grossProfit = currentRevenue - totalCogs;
        decimal? grossMarginPct = currentRevenue > 0 ? Math.Round(grossProfit / currentRevenue * 100, 2) : null;

        var profitByCategory = catMap
            .Select(kv =>
            {
                var profit = kv.Value.Revenue - kv.Value.Cogs;
                return new CategoryProfitRow
                {
                    CategoryId = kv.Key,
                    CategoryName = kv.Value.Name,
                    Revenue = Math.Round(kv.Value.Revenue, 2),
                    CostOfGoods = Math.Round(kv.Value.Cogs, 2),
                    GrossProfit = Math.Round(profit, 2),
                    GrossProfitMarginPct = kv.Value.Revenue > 0 ? Math.Round(profit / kv.Value.Revenue * 100, 2) : null
                };
            })
            .OrderByDescending(x => x.GrossProfit)
            .ToList();

        return ApiResponse<FinancialMetricsResponse>.Ok(new FinancialMetricsResponse
        {
            CurrentPeriod = new MetricPeriod { From = from, To = to },
            PreviousPeriod = new MetricPeriod { From = prevFrom, To = prevTo },
            Revenue = new MetricItemDecimal
            {
                Current = Math.Round(currentRevenue, 2),
                Previous = Math.Round(previousRevenue, 2),
                Change = Math.Round(revenueChange, 2),
                PercentageChange = revenuePct
            },
            CostOfGoods = Math.Round(totalCogs, 2),
            GrossProfit = Math.Round(grossProfit, 2),
            GrossProfitMarginPct = grossMarginPct,
            ProfitByCategory = profitByCategory
        });
    }

    // ── Private helper ─────────────────────────────────────────────────────────
    private async Task<List<long>> ResolveStoreIdsAsync(long businessId, long? storeId, CancellationToken cancellationToken)
    {
        if (storeId.HasValue)
            return [storeId.Value];

        return await unitOfWork.Query<Store>()
            .Where(x => x.BusinessId == businessId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }
}
