using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class StockTransferService(IUnitOfWork unitOfWork) : IStockTransferService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<StockTransferResponse>> CreateAsync(CreateStockTransferRequest request, long userId, CancellationToken cancellationToken = default)
    {
        if (request.SourceStoreId == request.DestinationStoreId)
        {
            return ApiResponse<StockTransferResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "Source and destination stores must be different",
                [new ApiError("SameStore", "Source and destination store cannot be the same", nameof(request.DestinationStoreId))]);
        }

        // Validate both stores belong to the user's business
        var stores = await unitOfWork.Query<Store>()
            .Include(x => x.Business)
            .Where(x => x.Id == request.SourceStoreId || x.Id == request.DestinationStoreId)
            .ToListAsync(cancellationToken);

        var sourceStore = stores.FirstOrDefault(x => x.Id == request.SourceStoreId);
        var destinationStore = stores.FirstOrDefault(x => x.Id == request.DestinationStoreId);

        if (sourceStore is null || sourceStore.Business.UserId != userId)
        {
            return ApiResponse<StockTransferResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "Source store not found or does not belong to your business",
                [new ApiError("UnauthorizedStore", "Source store does not belong to your business", nameof(request.SourceStoreId))]);
        }

        if (destinationStore is null || destinationStore.Business.UserId != userId)
        {
            return ApiResponse<StockTransferResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "Destination store not found or does not belong to your business",
                [new ApiError("UnauthorizedStore", "Destination store does not belong to your business", nameof(request.DestinationStoreId))]);
        }

        // Load all source products
        var sourceProductIds = request.Items.Select(x => x.SourceProductId).ToList();

        var sourceProducts = await unitOfWork.Query<Product>()
            .Where(x => sourceProductIds.Contains(x.Id) && x.StoreId == request.SourceStoreId && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var sourceProductMap = sourceProducts.ToDictionary(x => x.Id);

        // Validate all source products exist
        var missingSource = sourceProductIds.Where(id => !sourceProductMap.ContainsKey(id)).ToList();
        if (missingSource.Count > 0)
        {
            return ApiResponse<StockTransferResponse>.Failed(
                StatusCodes.Status404NotFound,
                "One or more source products not found in the source store",
                missingSource.Select(id => new ApiError("ProductNotFound", $"Source product with id {id} not found", nameof(StockTransferItemRequest.SourceProductId))));
        }

        // Auto-match destination products by name; create missing ones from source
        var now = DateTime.UtcNow;
        var sourceNames = sourceProducts.Select(x => x.Name.ToLower()).ToList();
        var existingDestinationProducts = await unitOfWork.Query<Product>()
            .Where(x => sourceNames.Contains(x.Name.ToLower()) && x.StoreId == request.DestinationStoreId && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var destinationProductMap = existingDestinationProducts
            .ToDictionary(x => x.Name.ToLower());

        // Create any missing products in the destination store
        var productsToCreate = sourceProducts
            .Where(x => !destinationProductMap.ContainsKey(x.Name.ToLower()))
            .ToList();

        foreach (var src in productsToCreate)
        {
            var newProduct = new Product
            {
                Name = src.Name,
                Price = src.Price,
                Quantity = 0,
                StoreId = request.DestinationStoreId,
                ProductCategoryId = src.ProductCategoryId,
                Description = src.Description,
                Sku = src.Sku,
                Barcode = src.Barcode,
                CostPrice = src.CostPrice,
                CompareAtPrice = src.CompareAtPrice,
                AvailableOnPos = src.AvailableOnPos,
                AvailableOnAutShop = src.AvailableOnAutShop,
                ReorderThreshold = src.ReorderThreshold,
                SupplierId = src.SupplierId,
                IsActive = true,
                CreatedBy = SystemActor,
                DateCreated = now
            };

            await unitOfWork.CreateAsync(newProduct, cancellationToken);
            destinationProductMap[src.Name.ToLower()] = newProduct;
        }

        // Validate sufficient stock in source
        var insufficientStock = request.Items
            .Where(x => sourceProductMap[x.SourceProductId].Quantity < x.Quantity)
            .Select(x => new ApiError(
                "InsufficientStock",
                $"Insufficient stock for '{sourceProductMap[x.SourceProductId].Name}'. Available: {sourceProductMap[x.SourceProductId].Quantity}, Requested: {x.Quantity}",
                nameof(StockTransferItemRequest.Quantity)))
            .ToList();

        if (insufficientStock.Count > 0)
        {
            return ApiResponse<StockTransferResponse>.Failed(
                StatusCodes.Status409Conflict,
                "One or more products have insufficient stock for transfer",
                insufficientStock);
        }

        var transferDate = request.TransferDate.HasValue
            ? request.TransferDate.Value.ToUniversalTime()
            : now;

        // Build transfer items and adjust stock
        var transferItems = new List<StockTransferItem>();

        foreach (var item in request.Items)
        {
            var sourceProduct = sourceProductMap[item.SourceProductId];
            var destinationProduct = destinationProductMap[sourceProduct.Name.ToLower()];

            sourceProduct.Quantity -= item.Quantity;
            sourceProduct.DateUpdated = now;
            sourceProduct.UpdatedBy = SystemActor;
            unitOfWork.Update(sourceProduct);

            destinationProduct.Quantity += item.Quantity;
            destinationProduct.DateUpdated = now;
            destinationProduct.UpdatedBy = SystemActor;
            unitOfWork.Update(destinationProduct);

            transferItems.Add(new StockTransferItem
            {
                SourceProductId = item.SourceProductId,
                DestinationProductId = destinationProduct.Id,
                Quantity = item.Quantity,
                Notes = null,
                IsActive = true,
                CreatedBy = SystemActor,
                DateCreated = now
            });
        }

        var transfer = new StockTransfer
        {
            TransferNumber = GenerateTransferNumber(),
            SourceStoreId = request.SourceStoreId,
            DestinationStoreId = request.DestinationStoreId,
            TransferDate = transferDate,
            Status = StockTransferStatus.Completed,
            Notes = request.Notes,
            Items = transferItems,
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = now
        };

        await unitOfWork.CreateAsync(transfer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await unitOfWork.Query<StockTransfer>()
            .Include(x => x.SourceStore)
            .Include(x => x.DestinationStore)
            .Include(x => x.Items).ThenInclude(x => x.SourceProduct)
            .Include(x => x.Items).ThenInclude(x => x.DestinationProduct)
            .FirstAsync(x => x.Id == transfer.Id, cancellationToken);

        return ApiResponse<StockTransferResponse>.Created(MapToResponse(created), "Stock transfer completed successfully");
    }

    public async Task<ApiResponse<StockTransferResponse>> GetByIdAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var transfer = await unitOfWork.Query<StockTransfer>()
            .Include(x => x.SourceStore).ThenInclude(x => x.Business)
            .Include(x => x.DestinationStore)
            .Include(x => x.Items).ThenInclude(x => x.SourceProduct)
            .Include(x => x.Items).ThenInclude(x => x.DestinationProduct)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (transfer is null)
        {
            return ApiResponse<StockTransferResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Stock transfer not found",
                [new ApiError("TransferNotFound", "No stock transfer found for this id", nameof(id))]);
        }

        if (transfer.SourceStore.Business.UserId != userId)
        {
            return ApiResponse<StockTransferResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this stock transfer",
                [new ApiError("UnauthorizedTransfer", "This transfer does not belong to your business", nameof(id))]);
        }

        return ApiResponse<StockTransferResponse>.Ok(MapToResponse(transfer));
    }

    public async Task<ApiResponse<PagedResponse<StockTransferResponse>>> GetAllAsync(PagedQueryRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = unitOfWork.Query<StockTransfer>()
            .Include(x => x.SourceStore).ThenInclude(x => x.Business)
            .Include(x => x.DestinationStore)
            .Include(x => x.Items).ThenInclude(x => x.SourceProduct)
            .Include(x => x.Items).ThenInclude(x => x.DestinationProduct)
            .Where(x => x.SourceStore.Business.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.TransferNumber.ToLower().Contains(search) ||
                x.SourceStore.Name.ToLower().Contains(search) ||
                x.DestinationStore.Name.ToLower().Contains(search));
        }

        if (request.Filters is not null)
        {
            if (request.Filters.TryGetValue("sourceStoreId", out var srcFilter) && long.TryParse(srcFilter, out var srcId))
                query = query.Where(x => x.SourceStoreId == srcId);

            if (request.Filters.TryGetValue("destinationStoreId", out var dstFilter) && long.TryParse(dstFilter, out var dstId))
                query = query.Where(x => x.DestinationStoreId == dstId);

            if (request.Filters.TryGetValue("status", out var statusFilter) &&
                Enum.TryParse<StockTransferStatus>(statusFilter, true, out var status))
                query = query.Where(x => x.Status == status);

            if (request.Filters.TryGetValue("startDate", out var startStr) && DateTime.TryParse(startStr, out var startDate))
                query = query.Where(x => x.TransferDate.Date >= startDate.Date);

            if (request.Filters.TryGetValue("endDate", out var endStr) && DateTime.TryParse(endStr, out var endDate))
                query = query.Where(x => x.TransferDate.Date <= endDate.Date);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToResponse(x))
            .ToListAsync(cancellationToken);

        var paged = new PagedResponse<StockTransferResponse>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items
        };

        return ApiResponse<PagedResponse<StockTransferResponse>>.Ok(paged);
    }

    private static string GenerateTransferNumber()
        => $"TRF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    private static StockTransferResponse MapToResponse(StockTransfer transfer) => new()
    {
        TransferId = transfer.Id,
        TransferNumber = transfer.TransferNumber,
        SourceStoreId = transfer.SourceStoreId,
        SourceStoreName = transfer.SourceStore.Name,
        DestinationStoreId = transfer.DestinationStoreId,
        DestinationStoreName = transfer.DestinationStore.Name,
        TransferDate = transfer.TransferDate,
        Status = transfer.Status,
        Notes = transfer.Notes,
        Items = transfer.Items.Select(x => new StockTransferItemResponse
        {
            ItemId = x.Id,
            SourceProductId = x.SourceProductId,
            SourceProductName = x.SourceProduct.Name,
            DestinationProductId = x.DestinationProductId,
            DestinationProductName = x.DestinationProduct.Name,
            Quantity = x.Quantity,
            Notes = x.Notes
        }).ToList(),
        CreatedAt = transfer.DateCreated
    };
}
