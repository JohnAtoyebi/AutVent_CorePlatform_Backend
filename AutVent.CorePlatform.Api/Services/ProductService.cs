using System.Text.Json;
using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
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
                Supplier = NormalizeNullableString(x.Supplier)
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
                    x.Supplier,
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
            .Where(x => requestNames.Contains(x.Name.ToLower()) && requestStoreIds.Contains(x.StoreId))
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
                Sku = x.Sku ?? GenerateSku(x.Name),
                Barcode = x.Barcode,
                CostPrice = x.CostPrice,
                CompareAtPrice = x.CompareAtPrice,
                ProductImagesJson = SerializeStringList(x.ProductImages),
                ProductVariantsEnabled = x.ProductVariantsEnabled,
                ProductVariantsJson = SerializeVariants(x.ProductVariants),
                IsActive = x.ActiveProduct ?? true,
                AvailableOnPos = x.AvailableOnPos ?? true,
                AvailableOnAutShop = x.AvailableOnAutShop ?? true,
                ReorderThreshold = x.ReorderThreshold,
                ApplyToAllStoreLocations = x.ApplyToAllStoreLocations,
                TagsJson = SerializeStringList(x.Tags),
                Weight = x.Weight,
                Supplier = x.Supplier,
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
            .Where(x => requestNames.Contains(x.Name.ToLower()) && x.StoreId == storeId)
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
                    Sku = GenerateSku(x.Name),
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
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

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
            .Where(x => x.Store.Business.UserId == userId)
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
            ProductSortBy.Oldest      => query.OrderBy(x => x.Id),
            ProductSortBy.NameAsc     => query.OrderBy(x => x.Name),
            ProductSortBy.NameDesc    => query.OrderByDescending(x => x.Name),
            ProductSortBy.QuantityAsc => query.OrderBy(x => x.Quantity),
            ProductSortBy.QuantityDesc => query.OrderByDescending(x => x.Quantity),
            _                         => query.OrderByDescending(x => x.Id)
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

    private static string GenerateSku(string productName)
    {
        var prefix = new string(productName
            .Where(char.IsLetterOrDigit)
            .Take(3)
            .ToArray())
            .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(prefix))
        {
            prefix = "PRD";
        }

        var suffix = Guid.NewGuid().ToString("N").ToUpperInvariant();
        return $"{prefix}{suffix}"[..10];
    }

    private static string NormalizePriceString(string value)
        => value.Replace(",", string.Empty).Trim();

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
        Supplier = product.Supplier
    };

    public async Task<ApiResponse<IReadOnlyCollection<ProductResponse>>> UpdateAsync(long id, CreateProductRequest request, long userId, long storeId, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Query<Product>()
            .Include(x => x.Store)
            .ThenInclude(x => x.Business)
            .Include(x => x.ProductCategory)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

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
                    x.Name.ToLower() == product.Name.ToLower())
                .ToListAsync(cancellationToken)
            : await unitOfWork.Query<Product>()
                .Include(x => x.ProductCategory)
                .Where(x => x.Id == id && targetStoreIds.Contains(x.StoreId))
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
                !targetProductIds.Contains(x.Id))
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
        var normalizedSupplier = NormalizeNullableString(request.Supplier);
        var normalizedImages = NormalizeStringList(request.ProductImages);
        var normalizedVariants = NormalizeVariants(request.ProductVariants);
        var normalizedTags = NormalizeStringList(request.Tags);

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

            targetProduct.Sku = normalizedSku ?? targetProduct.Sku ?? GenerateSku(normalizedName);

            if (request.Barcode is not null)
                targetProduct.Barcode = normalizedBarcode;

            if (request.CostPrice is not null)
                targetProduct.CostPrice = normalizedCostPrice;

            if (request.CompareAtPrice is not null)
                targetProduct.CompareAtPrice = normalizedCompareAtPrice;

            if (request.ProductImages is not null)
                targetProduct.ProductImagesJson = SerializeStringList(normalizedImages);

            if (request.ProductVariantsEnabled.HasValue)
                targetProduct.ProductVariantsEnabled = request.ProductVariantsEnabled;

            if (request.ProductVariants is not null)
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

            if (request.Supplier is not null)
                targetProduct.Supplier = normalizedSupplier;

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
        product.DateDeleted = now;
        product.DateUpdated = now;
        product.UpdatedBy = SystemActor;

        unitOfWork.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Product deleted successfully");
    }

    public async Task<ApiResponse<bool>> UpdateStatusAsync(long id, bool isActive, long userId, CancellationToken cancellationToken = default)
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

        if (product.IsActive == isActive)
        {
            var message = isActive ? "Product is already active" : "Product is already inactive";
            return ApiResponse<bool>.Ok(true, message);
        }

        var now = DateTime.UtcNow;
        product.IsActive = isActive;
        product.DateDeleted = isActive ? null : now;
        product.DateUpdated = now;
        product.UpdatedBy = SystemActor;

        unitOfWork.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var successMessage = isActive ? "Product activated successfully" : "Product deactivated successfully";
        return ApiResponse<bool>.Ok(true, successMessage);
    }
}
