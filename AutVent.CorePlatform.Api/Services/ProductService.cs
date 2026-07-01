using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class ProductService(IUnitOfWork unitOfWork) : IProductService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<IReadOnlyCollection<ProductResponse>>> CreateAsync(IReadOnlyCollection<CreateProductRequest> requests, CancellationToken cancellationToken = default)
    {
        if (requests.Count == 0)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status400BadRequest,
                "At least one product is required",
                [new ApiError("EmptyPayload", "Provide one or more products")]);
        }

        var now = DateTime.UtcNow;
        var normalizedRequests = requests
            .Select(x => new
            {
                Name = x.Name.Trim(),
                Price = x.Price.Trim(),
                Quantity = x.Quantity,
                Category = x.ProductCategory.Trim()
            })
            .ToList();

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
            .Where(x => requestNames.Contains(x.Name.ToLower()))
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        if (existingNames.Count > 0)
        {
            return ApiResponse<IReadOnlyCollection<ProductResponse>>.Failed(
                StatusCodes.Status409Conflict,
                "One or more products already exist",
                existingNames.Select(name => new ApiError("DuplicateProduct", $"Product already exists: {name}", nameof(CreateProductRequest.Name))));
        }

        var categoryNames = normalizedRequests
            .Select(x => x.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var existingCategories = await unitOfWork.Query<ProductCategory>()
            .Where(x => categoryNames.Contains(x.Name))
            .ToListAsync(cancellationToken);

        var categoryMap = existingCategories.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var categoryName in categoryNames)
        {
            if (categoryMap.ContainsKey(categoryName))
            {
                continue;
            }

            var category = new ProductCategory
            {
                Name = categoryName,
                IsActive = true,
                CreatedBy = SystemActor,
                DateCreated = now
            };

            await unitOfWork.CreateAsync(category, cancellationToken);
            categoryMap[categoryName] = category;
        }

        var products = normalizedRequests
            .Select(x => new Product
            {
                Name = x.Name,
                Price = x.Price,
                Quantity = x.Quantity,
                ProductCategory = categoryMap[x.Category],
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

    public async Task<ApiResponse<IReadOnlyCollection<ProductResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await unitOfWork.Query<Product>()
            .Include(x => x.ProductCategory)
            .Select(x => new ProductResponse
            {
                ProductId = x.Id,
                Name = x.Name,
                Price = x.Price,
                Quantity = x.Quantity,
                ProductCategory = x.ProductCategory.Name
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<ProductResponse>>.Ok(items);
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
