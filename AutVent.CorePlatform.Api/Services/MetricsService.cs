using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class MetricsService(IUnitOfWork unitOfWork) : IMetricsService
{
    private const long DefaultLowStockThreshold = 10;

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
            PercentageChange = pct
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

        // Load all active products in scope (current snapshot)
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

        // ── Total Stock Value ──────────────────────────────────────────
        static decimal StockValue(decimal qty, string? costPriceStr)
        {
            if (!decimal.TryParse(costPriceStr, out var cost)) return 0m;
            return qty * cost;
        }

        var currentStockValue = products
            .Sum(p => StockValue(p.Quantity, p.CostPrice));

        var prevProducts = products.Where(p => p.DateCreated < prevTo).ToList();
        var previousStockValue = prevProducts
            .Sum(p => StockValue(p.Quantity, p.CostPrice));

        var stockValueChange = currentStockValue - previousStockValue;
        decimal? stockValuePct = previousStockValue > 0
            ? Math.Round(stockValueChange / previousStockValue * 100, 2)
            : currentStockValue > 0 ? 100m : null;

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
}
