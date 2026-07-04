using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class ProductService(IUnitOfWork unitOfWork) : IProductService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<IReadOnlyCollection<ProductResponse>>> CreateAsync(IReadOnlyCollection<CreateProductRequest> requests, long userId, long storeId, CancellationToken cancellationToken = default)
    {
        if (requests.Count == 0)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status400BadRequest,
                "At least one product is required",
                [new ApiError("EmptyPayload", "Provide one or more products")]);
        }

        // Validate store exists and belongs to the user
        var store = await unitOfWork.Query<Store>()
            .Include(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == storeId, cancellationToken);

        if (store is null)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status404NotFound,
                "Store not found",
                [new ApiError("StoreNotFound", "No store found for this id", nameof(storeId))]);
        }

        if (store.Business.UserId != userId)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status409Conflict,
                "Store does not belong to the current user",
                [new ApiError("UnauthorizedStore", "The store does not belong to the current user", nameof(storeId))]);
        }

        var now = DateTime.UtcNow;
        var normalizedRequests = requests
            .Select(x => new
            {
                Name = x.Name.Trim(),
                Price = x.Price.Trim(),
                Quantity = x.Quantity,
                ProductCategoryId = x.ProductCategoryId
            })
            .ToList();

        // Validate no product has zero quantity
        var zeroQuantityProducts = normalizedRequests
            .Where(x => x.Quantity <= 0)
            .Select(x => x.Name)
            .ToArray();

        if (zeroQuantityProducts.Length > 0)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status400BadRequest,
                "Product quantity must be greater than 0",
                zeroQuantityProducts.Select(name => new ApiError("InvalidQuantity", $"Product '{name}' has invalid quantity. Quantity must be greater than 0", nameof(CreateProductRequest.Quantity))));
        }

        var duplicatePayloadNames = normalizedRequests
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        if (duplicatePayloadNames.Length > 0)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status400BadRequest,
                "Duplicate product names in request",
                duplicatePayloadNames.Select(name => new ApiError("DuplicatePayloadProduct", $"Duplicate product in payload: {name}", nameof(CreateProductRequest.Name))));
        }

        var requestNames = normalizedRequests.Select(x => x.Name.ToLower()).ToArray();

        var existingNames = await unitOfWork.Query<Product>()
            .Where(x => requestNames.Contains(x.Name.ToLower()) && x.StoreId == storeId)
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        if (existingNames.Count > 0)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status409Conflict,
                "One or more products already exist in this store",
                existingNames.Select(name => new ApiError("DuplicateProduct", $"Product already exists in this store: {name}", nameof(CreateProductRequest.Name))));
        }

        var categoryIds = normalizedRequests
            .Select(x => x.ProductCategoryId)
            .Distinct()
            .ToArray();

        var existingCategories = await unitOfWork.Query<ProductCategory>()
            .Where(x => categoryIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var categoryMap = existingCategories.ToDictionary(x => x.Id);

        // Validate all requested categories exist
        var missingCategoryIds = categoryIds.Where(id => !categoryMap.ContainsKey(id)).ToArray();
        if (missingCategoryIds.Length > 0)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status404NotFound,
                "One or more product categories do not exist",
                missingCategoryIds.Select(id => new ApiError("InvalidCategory", $"Product category with id {id} does not exist", nameof(CreateProductRequest.ProductCategoryId))));
        }

        var products = normalizedRequests
            .Select(x => new Product
            {
                Name = x.Name,
                Price = x.Price,
                Quantity = x.Quantity,
                StoreId = storeId,
                ProductCategoryId = x.ProductCategoryId,
                ProductCategory = categoryMap[x.ProductCategoryId],
                IsActive = true,
                CreatedBy = SystemActor,
                DateCreated = now
            })
            .ToArray();

        await unitOfWork.CreateRangeAsync(products, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = products
            .Select(x => MapToResponse(x, x.ProductCategory.Name))
            .ToArray();

        return ApiResponse<IReadOnlyCollection<ProductResponse>>.Created(response, "Products created successfully");
    }

    public async Task<ApiResponse<ProductImportResponse>> ImportAsync(IFormFile file, long userId, long storeId, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
        {
            return ApiResponse<ProductImportResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "File is empty",
                [new ApiError("EmptyFile", "Upload a non-empty excel file")]);
        }

        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<ProductImportResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "Invalid file format",
                [new ApiError("InvalidFileFormat", "Only .xlsx files are supported")]);
        }

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet is null)
        {
            return ApiResponse<ProductImportResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "Workbook is empty",
                [new ApiError("EmptyWorkbook", "No worksheet found in the uploaded file")]);
        }

        var requests = new List<CreateProductRequest>();
        var errors = new List<ApiError>();
        var rowNumber = 2;

        while (!worksheet.Row(rowNumber).IsEmpty())
        {
            var name = worksheet.Cell(rowNumber, 1).GetString().Trim();
            var price = worksheet.Cell(rowNumber, 2).GetString().Trim();
            var quantityRaw = worksheet.Cell(rowNumber, 3).GetString().Trim();
            var categoryIdRaw = worksheet.Cell(rowNumber, 4).GetString().Trim();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(price) ||
                string.IsNullOrWhiteSpace(quantityRaw) ||
                string.IsNullOrWhiteSpace(categoryIdRaw))
            {
                errors.Add(new ApiError("InvalidRow", $"Row {rowNumber} has missing required values"));
                rowNumber++;
                continue;
            }

            if (!long.TryParse(quantityRaw, out var quantity))
            {
                errors.Add(new ApiError("InvalidQuantity", $"Row {rowNumber} has invalid quantity value"));
                rowNumber++;
                continue;
            }

            if (quantity <= 0)
            {
                errors.Add(new ApiError("InvalidQuantity", $"Row {rowNumber} has invalid quantity. Quantity must be greater than 0"));
                rowNumber++;
                continue;
            }

            if (!long.TryParse(categoryIdRaw, out var categoryId))
            {
                errors.Add(new ApiError("InvalidCategoryId", $"Row {rowNumber} has invalid category id"));
                rowNumber++;
                continue;
            }

            requests.Add(new CreateProductRequest
            {
                Name = name,
                Price = price,
                Quantity = quantity,
                ProductCategoryId = categoryId
            });

            rowNumber++;
        }

        if (errors.Count > 0)
        {
            return ApiResponse<ProductImportResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "Invalid excel content",
                errors);
        }

        var createResponse = await CreateAsync(requests, userId, storeId, cancellationToken);
        if (!createResponse.Success)
        {
            return ApiResponse<ProductImportResponse>.Failed(
                createResponse.StatusCode,
                createResponse.Message,
                createResponse.Errors);
        }

        var response = new ProductImportResponse
        {
            ImportedCount = createResponse.Data?.Count ?? 0,
            ImportedProducts = createResponse.Data ?? []
        };

        return ApiResponse<ProductImportResponse>.Created(response, "Products imported successfully");
    }

    public async Task<ApiResponse<ProductResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Query<Product>()
            .Include(x => x.ProductCategory)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (product is null)
        {
            return ApiResponse<ProductResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Product not found",
                [new ApiError("ProductNotFound", "No product found for this id", nameof(id))]);
        }

        return ApiResponse<ProductResponse>.Ok(MapToResponse(product, product.ProductCategory.Name));
    }

    public async Task<ApiResponse<PagedResponse<ProductResponse>>> GetAllAsync(PagedQueryRequest request, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = unitOfWork.Query<Product>()
            .Include(x => x.ProductCategory)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(search) ||
                x.ProductCategory.Name.ToLower().Contains(search));
        }

        if (request.Filters is not null)
        {
            if (request.Filters.TryGetValue("productCategory", out var categoryFilter) && !string.IsNullOrWhiteSpace(categoryFilter))
            {
                var category = categoryFilter.Trim().ToLower();
                query = query.Where(x => x.ProductCategory.Name.ToLower() == category);
            }

            if (request.Filters.TryGetValue("minQuantity", out var minQtyFilter) && long.TryParse(minQtyFilter, out var minQty))
            {
                query = query.Where(x => x.Quantity >= minQty);
            }

            if (request.Filters.TryGetValue("maxQuantity", out var maxQtyFilter) && long.TryParse(maxQtyFilter, out var maxQty))
            {
                query = query.Where(x => x.Quantity <= maxQty);
            }

            if (request.Filters.TryGetValue("isActive", out var isActiveFilter) && bool.TryParse(isActiveFilter, out var isActive))
            {
                query = query.Where(x => x.IsActive == isActive);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProductResponse
            {
                ProductId = x.Id,
                Name = x.Name,
                Price = x.Price,
                Quantity = x.Quantity,
                ProductCategory = x.ProductCategory.Name
            })
            .ToListAsync(cancellationToken);

        var paged = new PagedResponse<ProductResponse>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items
        };

        return ApiResponse<PagedResponse<ProductResponse>>.Ok(paged);
    }

    private static ProductResponse MapToResponse(Product product, string categoryName) => new()
    {
        ProductId = product.Id,
        Name = product.Name,
        Price = product.Price,
        Quantity = product.Quantity,
        ProductCategory = categoryName
    };
}
