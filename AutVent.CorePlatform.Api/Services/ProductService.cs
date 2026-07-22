using System.Text.Json;
using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class ProductService(IUnitOfWork unitOfWork, IImageService imageService, IAuditLogService auditLogService) : IProductService
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

        var businessStoreIds = await unitOfWork.Query<Store>()
            .Where(x => x.BusinessId == store.BusinessId)
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);

        var normalizedRequests = requests
            .Select(x => new
            {
                Name = x.Name.Trim(),
                Price = NormalizePriceString(x.Price),
                Quantity = x.Quantity,
                ProductCategoryId = x.ProductCategoryId,
                Description = NormalizeNullableString(x.Description),
                Sku = NormalizeNullableString(x.Sku),
                Barcode = NormalizeNullableString(x.Barcode),
                CostPrice = NormalizeNullablePriceString(x.CostPrice),
                CompareAtPrice = NormalizeNullablePriceString(x.CompareAtPrice),
                ProductImages = NormalizeStringList(x.ProductImages),
                ProductVariantsEnabled = x.ProductVariantsEnabled,
                ProductVariants = NormalizeVariants(x.ProductVariants),
                ActiveProduct = x.ActiveProduct,
                AvailableOnPos = x.AvailableOnPos,
                AvailableOnAutShop = x.AvailableOnAutShop,
                ReorderThreshold = x.ReorderThreshold,
                StoreId = x.StoreId,
                ApplyToAllStoreLocations = x.ApplyToAllStoreLocations,
                Tags = NormalizeStringList(x.Tags),
                Weight = x.Weight,
                SupplierId = x.SupplierId
            })
            .ToList();

        var invalidStoreOverrides = normalizedRequests
            .Where(x => x.StoreId.HasValue && !businessStoreIds.Contains(x.StoreId.Value))
            .Select(x => x.StoreId!.Value)
            .Distinct()
            .ToArray();

        if (invalidStoreOverrides.Length > 0)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status400BadRequest,
                "Invalid target store location",
                invalidStoreOverrides.Select(id => new ApiError("InvalidStoreId", $"Store id {id} does not belong to your business", nameof(CreateProductRequest.StoreId))));
        }

        var expandedRequests = normalizedRequests
            .SelectMany(x =>
            {
                var targetStoreIds = x.ApplyToAllStoreLocations == true
                    ? businessStoreIds
                    : [x.StoreId ?? storeId];

                return targetStoreIds.Select(targetStoreId => new
                {
                    x.Name,
                    x.Price,
                    x.Quantity,
                    x.ProductCategoryId,
                    x.Description,
                    x.Sku,
                    x.Barcode,
                    x.CostPrice,
                    x.CompareAtPrice,
                    x.ProductImages,
                    x.ProductVariantsEnabled,
                    x.ProductVariants,
                    x.ActiveProduct,
                    x.AvailableOnPos,
                    x.AvailableOnAutShop,
                    x.ReorderThreshold,
                    x.ApplyToAllStoreLocations,
                    x.Tags,
                    x.Weight,
                    x.SupplierId,
                    TargetStoreId = targetStoreId
                });
            })
            .ToList();

        // Validate no product has zero quantity
        var zeroQuantityProducts = expandedRequests
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

        // Check for duplicates in payload
        var duplicatePayloadNames = expandedRequests
            .GroupBy(x => new { x.TargetStoreId, Name = x.Name.ToLowerInvariant() })
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        if (duplicatePayloadNames.Length > 0)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status400BadRequest,
                "Duplicate product names in request",
                duplicatePayloadNames.Select(x => new ApiError("DuplicatePayloadProduct", $"Duplicate product in payload for store {x.TargetStoreId}: {x.Name}", nameof(CreateProductRequest.Name))));
        }

        var requestNames = expandedRequests.Select(x => x.Name.ToLowerInvariant()).Distinct().ToArray();
        var requestStoreIds = expandedRequests.Select(x => x.TargetStoreId).Distinct().ToArray();
        var requestStoreNamePairs = expandedRequests
            .Select(x => $"{x.TargetStoreId}:{x.Name.ToLowerInvariant()}")
            .ToHashSet();

        var existingNames = await unitOfWork.Query<Product>()
            .Where(x => requestNames.Contains(x.Name.ToLower()) && requestStoreIds.Contains(x.StoreId) && !x.IsDeleted)
            .Select(x => new { x.Name, x.StoreId })
            .ToListAsync(cancellationToken);

        var conflictingProducts = existingNames
            .Where(x => requestStoreNamePairs.Contains($"{x.StoreId}:{x.Name.ToLowerInvariant()}"))
            .ToList();

        if (conflictingProducts.Count > 0)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status409Conflict,
                "One or more products already exist in the target store location",
                conflictingProducts.Select(x => new ApiError("DuplicateProduct", $"Product already exists in store {x.StoreId}: {x.Name}", nameof(CreateProductRequest.Name))));
        }

        var categoryIds = expandedRequests
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

        var invalidThresholds = expandedRequests
            .Where(x => x.ReorderThreshold.HasValue && x.ReorderThreshold.Value >= x.Quantity)
            .ToArray();

        if (invalidThresholds.Length > 0)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status400BadRequest,
                "Reorder threshold must be less than the stock quantity",
                invalidThresholds.Select(x => new ApiError(
                    "InvalidReorderThreshold",
                    $"Reorder threshold ({x.ReorderThreshold}) must be less than the stock quantity ({x.Quantity}) for product '{x.Name}'",
                    nameof(CreateProductRequest.ReorderThreshold))));
        }

        var requestedSupplierIds = expandedRequests
            .Where(x => x.SupplierId.HasValue)
            .Select(x => x.SupplierId!.Value)
            .Distinct()
            .ToArray();

        if (requestedSupplierIds.Length > 0)
        {
            var validSupplierIds = await unitOfWork.Query<Supplier>()
                .Where(x => requestedSupplierIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToHashSetAsync(cancellationToken);

            var invalidSupplierIds = requestedSupplierIds.Where(id => !validSupplierIds.Contains(id)).ToArray();
            if (invalidSupplierIds.Length > 0)
            {
                return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                    StatusCodes.Status404NotFound,
                    "One or more supplier IDs are invalid",
                    invalidSupplierIds.Select(id => new ApiError("InvalidSupplier", $"Supplier with id {id} does not exist or does not belong to your business", nameof(CreateProductRequest.SupplierId))));
            }
        }

        var products = expandedRequests
            .Select(x => new Product
            {
                Name = x.Name,
                Price = x.Price,
                Quantity = x.Quantity,
                StoreId = x.TargetStoreId,
                ProductCategoryId = x.ProductCategoryId,
                ProductCategory = categoryMap[x.ProductCategoryId],
                Description = x.Description,
                Sku = x.Sku ?? GenerateSkuInternal(x.Name),
                Barcode = x.Barcode,
                CostPrice = x.CostPrice,
                CompareAtPrice = x.CompareAtPrice,
                ProductImagesJson = SerializeStringList(x.ProductImages),
                ProductVariantsEnabled = x.ProductVariantsEnabled,
                ProductVariantsJson = x.ProductVariantsEnabled == true ? SerializeVariants(x.ProductVariants) : null,
                IsActive = x.ActiveProduct ?? true,
                AvailableOnPos = x.AvailableOnPos ?? true,
                AvailableOnAutShop = x.AvailableOnAutShop ?? true,
                ReorderThreshold = x.ReorderThreshold,
                ApplyToAllStoreLocations = x.ApplyToAllStoreLocations,
                TagsJson = SerializeStringList(x.Tags),
                Weight = x.Weight,
                SupplierId = x.SupplierId,
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

    private static ProductCategory GetBestMatchingCategory(string productName, IReadOnlyList<ProductCategory> availableCategories)
    {
        if (availableCategories.Count == 0)
            return null!;

        var productNameLower = productName.ToLower();
        var productWords = productNameLower.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

        var categoryScores = availableCategories
            .Select(category =>
            {
                var categoryNameLower = category.Name.ToLower();
                var categoryWords = categoryNameLower.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

                // Calculate match score based on word overlap
                var matchingWords = productWords.Count(pw => 
                    categoryWords.Any(cw => cw.Contains(pw) || pw.Contains(cw)));

                // Check if product name contains category name (substring match)
                var substringSimilarity = productNameLower.Contains(categoryNameLower) ? 10 : 0;
                if (substringSimilarity == 0 && categoryNameLower.Contains(productNameLower))
                    substringSimilarity = 8;

                var score = (matchingWords * 5) + substringSimilarity;

                return new { Category = category, Score = score };
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        // Return best match if score is reasonable, otherwise default to first category
        return categoryScores.FirstOrDefault()?.Score > 0 
            ? categoryScores.First().Category 
            : availableCategories[0];
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

        // Validate store exists and belongs to the user
        var store = await unitOfWork.Query<Store>()
            .Include(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == storeId, cancellationToken);

        if (store is null)
        {
            return ApiResponse<ProductImportResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Store not found",
                [new ApiError("StoreNotFound", "No store found for this id", nameof(storeId))]);
        }

        if (store.Business.UserId != userId)
        {
            return ApiResponse<ProductImportResponse>.Failed(
                StatusCodes.Status409Conflict,
                "Store does not belong to the current user",
                [new ApiError("UnauthorizedStore", "The store does not belong to the current user", nameof(storeId))]);
        }

        // Get all available categories for smart matching
        var allCategories = await unitOfWork.Query<ProductCategory>()
            .ToListAsync(cancellationToken);

        if (allCategories.Count == 0)
        {
            return ApiResponse<ProductImportResponse>.Failed(
                StatusCodes.Status404NotFound,
                "No product categories available",
                [new ApiError("NoCategoryAvailable", "System requires at least one product category to be available", nameof(ProductCategory))]);
        }

        var requests = new List<ImportProductRequest>();
        var errors = new List<ApiError>();
        var rowNumber = 2;

        while (!worksheet.Row(rowNumber).IsEmpty())
        {
            var name = worksheet.Cell(rowNumber, 1).GetString().Trim();
            var price = worksheet.Cell(rowNumber, 2).GetString().Trim();
            var quantityRaw = worksheet.Cell(rowNumber, 3).GetString().Trim();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(price) ||
                string.IsNullOrWhiteSpace(quantityRaw))
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

            requests.Add(new ImportProductRequest
            {
                Name = name,
                Price = price,
                Quantity = quantity
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

        var now = DateTime.UtcNow;
        var normalizedRequests = requests
            .Select(x => new
            {
                Name = x.Name.Trim(),
                Price = NormalizePriceString(x.Price),
                Quantity = x.Quantity
            })
            .ToList();

        // Validate no product has zero quantity
        var zeroQuantityProducts = normalizedRequests
            .Where(x => x.Quantity <= 0)
            .Select(x => x.Name)
            .ToArray();

        if (zeroQuantityProducts.Length > 0)
        {
            return ApiResponse<ProductImportResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "Product quantity must be greater than 0",
                zeroQuantityProducts.Select(name => new ApiError("InvalidQuantity", $"Product '{name}' has invalid quantity", nameof(ImportProductRequest.Quantity))));
        }

        // Check for duplicates in payload
        var duplicatePayloadNames = normalizedRequests
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        if (duplicatePayloadNames.Length > 0)
        {
            return ApiResponse<ProductImportResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "Duplicate product names in request",
                duplicatePayloadNames.Select(name => new ApiError("DuplicatePayloadProduct", $"Duplicate product in payload: {name}", nameof(ImportProductRequest.Name))));
        }

        var requestNames = normalizedRequests.Select(x => x.Name.ToLower()).ToArray();

        // Check if products already exist in store
        var existingNames = await unitOfWork.Query<Product>()
            .Where(x => requestNames.Contains(x.Name.ToLower()) && x.StoreId == storeId && !x.IsDeleted)
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        if (existingNames.Count > 0)
        {
            return ApiResponse<ProductImportResponse>.Failed(
                StatusCodes.Status409Conflict,
                "One or more products already exist in this store",
                existingNames.Select(name => new ApiError("DuplicateProduct", $"Product already exists in this store: {name}", nameof(ImportProductRequest.Name))));
        }

        // Create products with intelligently matched categories
        var products = normalizedRequests
            .Select(x =>
            {
                var matchedCategory = GetBestMatchingCategory(x.Name, allCategories);
                return new Product
                {
                    Name = x.Name,
                    Price = x.Price,
                    Quantity = x.Quantity,
                    StoreId = storeId,
                    ProductCategoryId = matchedCategory.Id,
                    ProductCategory = matchedCategory,
                    Sku = GenerateSkuInternal(x.Name),
                    IsActive = true,
                    AvailableOnPos = true,
                    AvailableOnAutShop = true,
                    CreatedBy = SystemActor,
                    DateCreated = now
                };
            })
            .ToArray();

        await unitOfWork.CreateRangeAsync(products, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = products
            .Select(x => MapToResponse(x, x.ProductCategory.Name))
            .ToArray();

        return ApiResponse<ProductImportResponse>.Created(
            new ProductImportResponse
            {
                ImportedCount = products.Length,
                ImportedProducts = response
            },
            "Products imported successfully");
    }

    public async Task<ApiResponse<ProductResponse>> GetByIdAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Query<Product>()
            .Include(x => x.Store)
            .ThenInclude(x => x.Business)
            .Include(x => x.ProductCategory)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (product is null)
        {
            return ApiResponse<ProductResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Product not found",
                [new ApiError("ProductNotFound", "No product found for this id", nameof(id))]);
        }

        if (product.Store.Business.UserId != userId)
        {
            return ApiResponse<ProductResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this product",
                [new ApiError("UnauthorizedProduct", "This product does not belong to your business", nameof(id))]);
        }

        return ApiResponse<ProductResponse>.Ok(MapToResponse(product, product.ProductCategory.Name));
    }

    public async Task<ApiResponse<PagedResponse<ProductResponse>>> GetAllAsync(PagedQueryRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = unitOfWork.Query<Product>()
            .Include(x => x.Store)
            .ThenInclude(x => x.Business)
            .Include(x => x.ProductCategory)
            .Where(x => x.Store.Business.UserId == userId && !x.IsDeleted)
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
            if (request.Filters.TryGetValue("storeId", out var storeIdFilter) && long.TryParse(storeIdFilter, out var filterStoreId))
            {
                var storeExists = await unitOfWork.Query<Store>()
                    .Include(x => x.Business)
                    .AnyAsync(x => x.Id == filterStoreId && x.Business.UserId == userId, cancellationToken);

                if (!storeExists)
                {
                    return ApiResponse<PagedResponse<ProductResponse>>.Failed(
                        StatusCodes.Status403Forbidden,
                        "You do not have access to this store",
                        [new ApiError("UnauthorizedStore", "This store does not belong to your business", nameof(filterStoreId))]);
                }

                query = query.Where(x => x.StoreId == filterStoreId);
            }

            // Filter by category name or category ID
            if (request.Filters.TryGetValue("productCategory", out var categoryFilter) && !string.IsNullOrWhiteSpace(categoryFilter))
            {
                var categoryLower = categoryFilter.Trim().ToLower();
                query = query.Where(x => x.ProductCategory.Name.ToLower() == categoryLower);
            }

            if (request.Filters.TryGetValue("productCategoryId", out var categoryIdFilter) && long.TryParse(categoryIdFilter, out var categoryId))
            {
                query = query.Where(x => x.ProductCategoryId == categoryId);
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

        var sortBy = Enum.TryParse<ProductSortBy>(request.SortBy, true, out var parsedSort)
            ? parsedSort
            : ProductSortBy.Newest;

        query = sortBy switch
        {
            ProductSortBy.Oldest       => query.OrderBy(x => x.DateCreated),
            ProductSortBy.NameAsc      => query.OrderBy(x => x.Name),
            ProductSortBy.NameDesc     => query.OrderByDescending(x => x.Name),
            ProductSortBy.QuantityAsc  => query.OrderBy(x => x.Quantity),
            ProductSortBy.QuantityDesc => query.OrderByDescending(x => x.Quantity),
            _                          => query.OrderByDescending(x => x.DateCreated)  
        };

        var records = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = records
            .Select(x => MapToResponse(x, x.ProductCategory.Name))
            .ToList();

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

    public ApiResponse<GenerateSkuResponse> GenerateSku(string productName)
    {
        var sku = GenerateSkuInternal(productName.Trim());
        return ApiResponse<GenerateSkuResponse>.Ok(new GenerateSkuResponse { Sku = sku });
    }

    private static string GenerateSkuInternal(string productName)
    {
        var prefix = new string(productName
            .Where(char.IsLetterOrDigit)
            .Take(3)
            .ToArray())
            .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(prefix))
        {
            prefix = "AUT";
        }

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var mid = new string(Enumerable.Range(0, 4).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        var end = new string(Enumerable.Range(0, 3).Select(_ => chars[random.Next(chars.Length)]).ToArray());

        return $"{prefix}-{mid}-{end}";
    }

    private static string NormalizePriceString(string value)
        => value.Replace(",", string.Empty).Trim();

    private static decimal? CalculateProfitMargin(string price, string? costPrice)
    {
        if (string.IsNullOrWhiteSpace(costPrice))
            return null;

        if (!decimal.TryParse(NormalizePriceString(price), out var sellingPrice) || sellingPrice <= 0)
            return null;

        if (!decimal.TryParse(NormalizePriceString(costPrice), out var cost))
            return null;

        return Math.Round((sellingPrice - cost) / sellingPrice * 100, 2);
    }

    private static string? NormalizeNullablePriceString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return NormalizePriceString(value);
    }

    private static string? NormalizeNullableString(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static List<string>? NormalizeStringList(List<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return null;
        }

        var cleaned = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return cleaned.Count == 0 ? null : cleaned;
    }

    private static List<CreateProductVariantRequest>? NormalizeVariants(List<CreateProductVariantRequest>? variants)
    {
        if (variants is null || variants.Count == 0)
        {
            return null;
        }

        var cleaned = variants
            .Select(v => new CreateProductVariantRequest
            {
                Variant = NormalizeNullableString(v.Variant),
                Sku = NormalizeNullableString(v.Sku),
                Price = NormalizeNullablePriceString(v.Price),
                Quantity = v.Quantity
            })
            .Where(v => v.Variant is not null || v.Sku is not null || v.Price is not null || v.Quantity.HasValue)
            .ToList();

        return cleaned.Count == 0 ? null : cleaned;
    }

    private static string? SerializeStringList(List<string>? items)
        => items is null ? null : JsonSerializer.Serialize(items);

    public async Task<ApiResponse<IReadOnlyCollection<ProductResponse>>> BulkEditAsync(BulkEditProductRequest request, long userId, long storeId, CancellationToken cancellationToken = default)
    {
        if (request.ProductIds.Count == 0)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status400BadRequest,
                "At least one product ID is required",
                [new ApiError("EmptyProductIds", "ProductIds must contain at least one item", nameof(request.ProductIds))]);
        }

        var store = await unitOfWork.Query<Store>()
            .Include(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == storeId, cancellationToken);

        if (store is null || store.Business.UserId != userId)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this store",
                [new ApiError("UnauthorizedStore", "This store does not belong to your business", nameof(storeId))]);
        }

        var products = await unitOfWork.Query<Product>()
            .Include(x => x.ProductCategory)
            .Where(x => request.ProductIds.Contains(x.Id) && x.StoreId == storeId && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var missingIds = request.ProductIds.Except(products.Select(x => x.Id)).ToList();
        if (missingIds.Count > 0)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status404NotFound,
                "One or more products not found in this store",
                missingIds.Select(id => new ApiError("ProductNotFound", $"Product with id {id} not found in this store", nameof(request.ProductIds))));
        }

        ProductCategory? newCategory = null;
        if (request.ProductCategoryId.HasValue)
        {
            newCategory = await unitOfWork.Query<ProductCategory>()
                .FirstOrDefaultAsync(x => x.Id == request.ProductCategoryId.Value, cancellationToken);

            if (newCategory is null)
            {
                return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                    StatusCodes.Status404NotFound,
                    "Product category not found",
                    [new ApiError("CategoryNotFound", "No category found for the provided id", nameof(request.ProductCategoryId))]);
            }
        }

        if (request.PriceAdjustment is not null &&
            (request.PriceAdjustment.Type == PriceAdjustmentType.PercentageIncrease ||
             request.PriceAdjustment.Type == PriceAdjustmentType.PercentageDecrease) &&
            request.PriceAdjustment.Value > 100)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status400BadRequest,
                "Percentage adjustment cannot exceed 100",
                [new ApiError("InvalidPercentage", "Percentage value must be between 0.01 and 100", nameof(request.PriceAdjustment.Value))]);
        }

        var now = DateTime.UtcNow;

        foreach (var product in products)
        {
            if (request.ProductCategoryId.HasValue && newCategory is not null)
            {
                product.ProductCategoryId = newCategory.Id;
                product.ProductCategory = newCategory;
            }

            if (request.IsActive.HasValue)
                product.IsActive = request.IsActive.Value;

            if (request.AvailableOnPos.HasValue)
                product.AvailableOnPos = request.AvailableOnPos.Value;

            if (request.SupplierId.HasValue)
                product.SupplierId = request.SupplierId;

            if (request.PriceAdjustment is not null &&
                decimal.TryParse(NormalizePriceString(product.Price), out var currentPrice))
            {
                var adjustment = request.PriceAdjustment;
                var newPrice = adjustment.Type switch
                {
                    PriceAdjustmentType.FixedIncrease      => currentPrice + adjustment.Value,
                    PriceAdjustmentType.FixedDecrease      => currentPrice - adjustment.Value,
                    PriceAdjustmentType.PercentageIncrease => currentPrice * (1 + adjustment.Value / 100),
                    PriceAdjustmentType.PercentageDecrease => currentPrice * (1 - adjustment.Value / 100),
                    _ => currentPrice
                };

                if (newPrice > 0)
                    product.Price = Math.Round(newPrice, 2).ToString("F2");
            }

            product.DateUpdated = now;
            product.UpdatedBy = SystemActor;
            unitOfWork.Update(product);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = products
            .Select(x => MapToResponse(x, x.ProductCategory.Name))
            .ToList();

        return ApiResponse<IReadOnlyCollection<ProductResponse>>.Ok(updated, $"{updated.Count} product(s) updated successfully");
    }

    private static string? SerializeVariants(List<CreateProductVariantRequest>? variants)
        => variants is null ? null : JsonSerializer.Serialize(variants);

    private static List<string>? DeserializeStringList(string? json)
        => string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<List<string>>(json);

    private static List<ProductVariantResponse>? DeserializeVariants(string? json)
        => string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<List<ProductVariantResponse>>(json);

    private static ProductResponse MapToResponse(Product product, string categoryName) => new()
    {
        ProductId = product.Id,
        StoreId = product.StoreId,
        Name = product.Name,
        Price = product.Price,
        Quantity = product.Quantity,
        ProductCategory = categoryName,
        Description = product.Description,
        Sku = product.Sku,
        Barcode = product.Barcode,
        CostPrice = product.CostPrice,
        CompareAtPrice = product.CompareAtPrice,
        ProductImages = DeserializeStringList(product.ProductImagesJson),
        ProductVariantsEnabled = product.ProductVariantsEnabled,
        ProductVariants = DeserializeVariants(product.ProductVariantsJson),
        ActiveProduct = product.IsActive,
        AvailableOnPos = product.AvailableOnPos,
        AvailableOnAutShop = product.AvailableOnAutShop,
        ReorderThreshold = product.ReorderThreshold,
        ApplyToAllStoreLocations = product.ApplyToAllStoreLocations,
        Tags = DeserializeStringList(product.TagsJson),
        Weight = product.Weight,
        SupplierId = product.SupplierId,
        ProfitMargin = CalculateProfitMargin(product.Price, product.CostPrice),
        CreatedAt = product.DateCreated,
        UpdatedAt = product.DateUpdated
    };

    public async Task<ApiResponse<IReadOnlyCollection<ProductResponse>>> UpdateAsync(long id, CreateProductRequest request, long userId, long storeId, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Query<Product>()
            .Include(x => x.Store)
            .ThenInclude(x => x.Business)
            .Include(x => x.ProductCategory)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (product is null)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status404NotFound,
                "Product not found",
                [new ApiError("ProductNotFound", "No product found for this id", nameof(id))]);
        }

        if (product.Store.Business.UserId != userId)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this product",
                [new ApiError("UnauthorizedProduct", "This product does not belong to your business", nameof(id))]);
        }

        var businessStoreIds = await unitOfWork.Query<Store>()
            .Where(x => x.BusinessId == product.Store.BusinessId)
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);

        var targetStoreIds = request.ApplyToAllStoreLocations == true
            ? businessStoreIds
            : [request.StoreId ?? storeId];

        var invalidTargetStores = targetStoreIds
            .Where(x => !businessStoreIds.Contains(x))
            .Distinct()
            .ToArray();

        if (invalidTargetStores.Length > 0)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status400BadRequest,
                "Invalid target store location",
                invalidTargetStores.Select(x => new ApiError("InvalidStoreId", $"Store id {x} does not belong to your business", nameof(CreateProductRequest.StoreId))));
        }

        var targetProducts = request.ApplyToAllStoreLocations == true
            ? await unitOfWork.Query<Product>()
                .Include(x => x.ProductCategory)
                .Where(x =>
                    x.Store.BusinessId == product.Store.BusinessId &&
                    targetStoreIds.Contains(x.StoreId) &&
                    x.Name.ToLower() == product.Name.ToLower() &&
                    !x.IsDeleted)
                .ToListAsync(cancellationToken)
            : await unitOfWork.Query<Product>()
                .Include(x => x.ProductCategory)
                .Where(x => x.Id == id && targetStoreIds.Contains(x.StoreId) && !x.IsDeleted)
                .ToListAsync(cancellationToken);

        if (targetProducts.Count == 0)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status404NotFound,
                "No product found for the target store location",
                [new ApiError("ProductNotFound", "No matching product found for the selected store location", nameof(request.StoreId))]);
        }

        if (request.Quantity <= 0)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status400BadRequest,
                "Product quantity must be greater than 0",
                [new ApiError("InvalidQuantity", "Quantity must be greater than 0", nameof(request.Quantity))]);
        }

        if (request.ReorderThreshold.HasValue && request.ReorderThreshold.Value >= request.Quantity)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status400BadRequest,
                "Reorder threshold must be less than the stock quantity",
                [new ApiError("InvalidReorderThreshold", $"Reorder threshold ({request.ReorderThreshold}) must be less than the stock quantity ({request.Quantity})", nameof(request.ReorderThreshold))]);
        }

        var normalizedName = request.Name.Trim();
        var normalizedPrice = NormalizePriceString(request.Price);

        var category = await unitOfWork.Query<ProductCategory>()
            .FirstOrDefaultAsync(x => x.Id == request.ProductCategoryId, cancellationToken);

        if (category is null)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status404NotFound,
                "Product category not found",
                [new ApiError("InvalidCategory", "Product category does not exist", nameof(request.ProductCategoryId))]);
        }

        var targetStoreIdSet = targetProducts.Select(x => x.StoreId).Distinct().ToArray();
        var targetProductIds = targetProducts.Select(x => x.Id).ToArray();

        var duplicateProducts = await unitOfWork.Query<Product>()
            .Where(x =>
                targetStoreIdSet.Contains(x.StoreId) &&
                x.Name.ToLower() == normalizedName.ToLower() &&
                !targetProductIds.Contains(x.Id) &&
                !x.IsDeleted)
            .Select(x => new { x.StoreId, x.Name })
            .ToListAsync(cancellationToken);

        if (duplicateProducts.Count > 0)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status409Conflict,
                "A product with this name already exists in one or more target store locations",
                duplicateProducts.Select(x => new ApiError("DuplicateProduct", $"Product already exists in store {x.StoreId}: {x.Name}", nameof(request.Name))));
        }

        var normalizedSku = NormalizeNullableString(request.Sku);
        var normalizedDescription = NormalizeNullableString(request.Description);
        var normalizedBarcode = NormalizeNullableString(request.Barcode);
        var normalizedCostPrice = NormalizeNullablePriceString(request.CostPrice);
        var normalizedCompareAtPrice = NormalizeNullablePriceString(request.CompareAtPrice);
        var normalizedImages = NormalizeStringList(request.ProductImages);
        var normalizedVariants = NormalizeVariants(request.ProductVariants);
        var normalizedTags = NormalizeStringList(request.Tags);

        if (request.SupplierId.HasValue)
        {
            var supplierExists = await unitOfWork.Query<Supplier>()
                .AnyAsync(x => x.Id == request.SupplierId.Value, cancellationToken);

            if (!supplierExists)
            {
                return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                    StatusCodes.Status404NotFound,
                    "Supplier not found",
                    [new ApiError("InvalidSupplier", $"Supplier with id {request.SupplierId} does not exist or does not belong to your business", nameof(request.SupplierId))]);
            }
        }

        var now = DateTime.UtcNow;
        foreach (var targetProduct in targetProducts)
        {
            targetProduct.Name = normalizedName;
            targetProduct.Price = normalizedPrice;
            targetProduct.Quantity = request.Quantity;
            targetProduct.ProductCategoryId = request.ProductCategoryId;
            targetProduct.ProductCategory = category;

            if (request.Description is not null)
                targetProduct.Description = normalizedDescription;

            targetProduct.Sku = normalizedSku ?? targetProduct.Sku ?? GenerateSkuInternal(normalizedName);

            if (request.Barcode is not null)
                targetProduct.Barcode = normalizedBarcode;

            if (request.CostPrice is not null)
                targetProduct.CostPrice = normalizedCostPrice;

            if (request.CompareAtPrice is not null)
                targetProduct.CompareAtPrice = normalizedCompareAtPrice;

            if (request.ProductImages is not null)
                targetProduct.ProductImagesJson = SerializeStringList(normalizedImages);

            if (request.ProductVariantsEnabled.HasValue)
            {
                targetProduct.ProductVariantsEnabled = request.ProductVariantsEnabled;

                if (request.ProductVariantsEnabled == false)
                    targetProduct.ProductVariantsJson = null;
                else if (request.ProductVariants is not null)
                    targetProduct.ProductVariantsJson = SerializeVariants(normalizedVariants);
            }
            else if (request.ProductVariants is not null && targetProduct.ProductVariantsEnabled == true)
                targetProduct.ProductVariantsJson = SerializeVariants(normalizedVariants);

            if (request.ActiveProduct.HasValue)
                targetProduct.IsActive = request.ActiveProduct.Value;

            if (request.AvailableOnPos.HasValue)
                targetProduct.AvailableOnPos = request.AvailableOnPos;

            if (request.AvailableOnAutShop.HasValue)
                targetProduct.AvailableOnAutShop = request.AvailableOnAutShop;

            if (request.ReorderThreshold.HasValue)
                targetProduct.ReorderThreshold = request.ReorderThreshold;

            if (request.ApplyToAllStoreLocations.HasValue)
                targetProduct.ApplyToAllStoreLocations = request.ApplyToAllStoreLocations;

            if (request.Tags is not null)
                targetProduct.TagsJson = SerializeStringList(normalizedTags);

            if (request.Weight.HasValue)
                targetProduct.Weight = request.Weight;

            if (request.SupplierId.HasValue)
                targetProduct.SupplierId = request.SupplierId;

            targetProduct.DateUpdated = now;
            targetProduct.UpdatedBy = SystemActor;
            unitOfWork.Update(targetProduct);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = targetProducts
            .Select(x => MapToResponse(x, x.ProductCategory.Name))
            .ToArray();

        return ApiResponse<IReadOnlyCollection<ProductResponse>>.Ok(response, "Product updated successfully");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Query<Product>()
            .Include(x => x.Store)
            .ThenInclude(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (product is null)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status404NotFound,
                "Product not found",
                [new ApiError("ProductNotFound", "No product found for this id", nameof(id))]);
        }

        if (product.Store.Business.UserId != userId)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this product",
                [new ApiError("UnauthorizedProduct", "This product does not belong to your business", nameof(id))]);
        }

        if (!product.IsActive)
        {
            return ApiResponse<bool>.Ok(true, "Product is already inactive");
        }

        var now = DateTime.UtcNow;
        product.IsActive = false;
        product.IsDeleted = true;
        product.DateDeleted = now;
        product.DateUpdated = now;
        product.UpdatedBy = SystemActor;

        unitOfWork.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            userId,
            AuditAction.ProductDeleted,
            nameof(Product),
            $"Product '{product.Name}' deleted.",
            entityId: product.Id,
            cancellationToken: cancellationToken);

        return ApiResponse<bool>.Ok(true, "Product deleted successfully");
    }

    public async Task<ApiResponse<bool>> UpdateStatusAsync(long id, bool isActive, long userId, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Query<Product>()
            .Include(x => x.Store)
            .ThenInclude(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (product is null)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status404NotFound,
                "Product not found",
                [new ApiError("ProductNotFound", "No product found for this id", nameof(id))]);
        }

        if (product.Store.Business.UserId != userId)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this product",
                [new ApiError("UnauthorizedProduct", "This product does not belong to your business", nameof(id))]);
        }

        if (product.IsActive == isActive)
        {
            var message = isActive ? "Product is already active" : "Product is already inactive";
            return ApiResponse<bool>.Ok(true, message);
        }

        var now = DateTime.UtcNow;
        product.IsActive = isActive;
        product.DateUpdated = now;
        product.UpdatedBy = SystemActor;

        unitOfWork.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var successMessage = isActive ? "Product activated successfully" : "Product deactivated successfully";
        return ApiResponse<bool>.Ok(true, successMessage);
    }

    public async Task<ApiResponse<ProductResponse>> UploadImagesAsync(long id, IReadOnlyCollection<IFormFile> files, long userId, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Query<Product>()
            .Include(x => x.Store)
            .ThenInclude(x => x.Business)
            .Include(x => x.ProductCategory)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (product is null)
        {
            return ApiResponse<ProductResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Product not found",
                [new ApiError("ProductNotFound", "No product found for this id", nameof(id))]);
        }

        if (product.Store.Business.UserId != userId)
        {
            return ApiResponse<ProductResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this product",
                [new ApiError("UnauthorizedProduct", "This product does not belong to your business", nameof(id))]);
        }

        if (files.Count == 0)
        {
            return ApiResponse<ProductResponse>.Failed(
                StatusCodes.Status400BadRequest,
                "At least one image file is required",
                [new ApiError("NoFiles", "Provide one or more image files", nameof(files))]);
        }

        var existingUrls = DeserializeStringList(product.ProductImagesJson) ?? [];

        var uploadTasks = files.Select(file => imageService.UploadAsync(file, userId, ImageType.Product, cancellationToken));
        var uploadResults = await Task.WhenAll(uploadTasks);

        var newUrls = existingUrls.Concat(uploadResults.Select(r => r.Url)).ToList();
        product.ProductImagesJson = SerializeStringList(newUrls);
        product.DateUpdated = DateTime.UtcNow;
        product.UpdatedBy = SystemActor;

        unitOfWork.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ProductResponse>.Ok(MapToResponse(product, product.ProductCategory.Name), "Images uploaded successfully");
    }

    public async Task<ApiResponse<ProductResponse>> DeleteImageAsync(long id, string imageUrl, long userId, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Query<Product>()
            .Include(x => x.Store)
            .ThenInclude(x => x.Business)
            .Include(x => x.ProductCategory)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (product is null)
        {
            return ApiResponse<ProductResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Product not found",
                [new ApiError("ProductNotFound", "No product found for this id", nameof(id))]);
        }

        if (product.Store.Business.UserId != userId)
        {
            return ApiResponse<ProductResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "You do not have access to this product",
                [new ApiError("UnauthorizedProduct", "This product does not belong to your business", nameof(id))]);
        }

        var existingUrls = DeserializeStringList(product.ProductImagesJson) ?? [];

        if (!existingUrls.Contains(imageUrl))
        {
            return ApiResponse<ProductResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Image not found on this product",
                [new ApiError("ImageNotFound", "The specified image URL does not exist on this product", nameof(imageUrl))]);
        }

        var publicId = ExtractCloudinaryPublicId(imageUrl);
        if (!string.IsNullOrWhiteSpace(publicId))
            await imageService.DeleteAsync(publicId, cancellationToken);

        var updatedUrls = existingUrls.Where(u => u != imageUrl).ToList();
        product.ProductImagesJson = SerializeStringList(updatedUrls);
        product.DateUpdated = DateTime.UtcNow;
        product.UpdatedBy = SystemActor;

        unitOfWork.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ProductResponse>.Ok(MapToResponse(product, product.ProductCategory.Name), "Image deleted successfully");
    }

    private static string? ExtractCloudinaryPublicId(string url)
    {
        // Cloudinary URL format: https://res.cloudinary.com/{cloud}/image/upload/{version}/{folder}/{filename}
        // Public ID = {folder}/{filename_without_extension}
        try
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var uploadIndex = Array.IndexOf(segments, "upload");
            if (uploadIndex < 0 || uploadIndex >= segments.Length - 1)
                return null;

            // Skip version segment (starts with 'v' followed by digits)
            var afterUpload = segments.Skip(uploadIndex + 1).ToArray();
            var start = afterUpload.Length > 0 && afterUpload[0].StartsWith('v') && afterUpload[0].Length > 1
                ? 1
                : 0;

            var publicIdWithExtension = string.Join("/", afterUpload.Skip(start));
            var dotIndex = publicIdWithExtension.LastIndexOf('.');
            return dotIndex > 0 ? publicIdWithExtension[..dotIndex] : publicIdWithExtension;
        }
        catch
        {
            return null;
        }
    }
}
