using System.Text.Json.Serialization;
using AutVent.Application.Abstractions.Api;
using AutVent.Domain.Entities;

namespace AutVent.Infrastructure.Api;

// ── Internal API shapes ──────────────────────────────────────────────────────

file sealed record PagedResponse<T>(
    [property: JsonPropertyName("items")] List<T> Items,
    [property: JsonPropertyName("totalCount")] int TotalCount,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("pageSize")] int PageSize);

file sealed record StoreApiItem(
    [property: JsonPropertyName("storeId")] long StoreId,
    [property: JsonPropertyName("businessId")] long BusinessId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("address")] string? Address,
    [property: JsonPropertyName("city")] string? City,
    [property: JsonPropertyName("phoneNumber")] string? PhoneNumber,
    [property: JsonPropertyName("storeCategory")] string? StoreCategory);

file sealed record ProductApiItem(
    [property: JsonPropertyName("productId")] long ProductId,
    [property: JsonPropertyName("storeId")] long StoreId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("barcode")] string? Barcode,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("costPrice")] decimal CostPrice,
    [property: JsonPropertyName("productCategory")] string? ProductCategory,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("reorderThreshold")] int ReorderThreshold,
    [property: JsonPropertyName("availableOnPos")] bool AvailableOnPos,
    [property: JsonPropertyName("activeProduct")] bool ActiveProduct,
    [property: JsonPropertyName("updatedAt")] DateTime? UpdatedAt);

// ── Client ───────────────────────────────────────────────────────────────────

public sealed class CatalogApiClient : ApiClientBase, ICatalogApiClient
{
    private readonly HttpClient _httpClient;

    public CatalogApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<Store>> GetStoresAsync(CancellationToken cancellationToken)
    {
        var stores = new List<Store>();
        var page = 1;
        const int pageSize = 100;

        while (true)
        {
            var response = await _httpClient.GetAsync($"api/Store?page={page}&pageSize={pageSize}", cancellationToken);
            var paged = await ReadApiResponseAsync<PagedResponse<StoreApiItem>>(response, cancellationToken);

            foreach (var item in paged.Items)
            {
                stores.Add(new Store
                {
                    Id = Guid.NewGuid(),
                    RemoteId = item.StoreId,
                    Name = item.Name,
                    Address = item.Address ?? string.Empty,
                    City = item.City ?? string.Empty,
                    PhoneNumber = item.PhoneNumber ?? string.Empty,
                    IsActive = true,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }

            if (stores.Count >= paged.TotalCount || paged.Items.Count < pageSize) break;
            page++;
        }

        return stores;
    }

    public async Task<IReadOnlyList<Product>> GetProductsAsync(long remoteStoreId, CancellationToken cancellationToken)
    {
        var products = new List<Product>();
        var page = 1;
        const int pageSize = 200;

        while (true)
        {
            var response = await _httpClient.GetAsync(
                //$"api/Product/store/{remoteStoreId}?page={page}&pageSize={pageSize}", cancellationToken);
                $"api/Product?PageNumber={page}&PageSize={pageSize}", cancellationToken);
            var paged = await ReadApiResponseAsync<PagedResponse<ProductApiItem>>(response, cancellationToken);

            foreach (var item in paged.Items)
            {
                products.Add(new Product
                {
                    Id = Guid.NewGuid(),
                    RemoteId = item.ProductId,
                    RemoteStoreId = item.StoreId,
                    Name = item.Name,
                    Sku = item.Sku.ToUpperInvariant(),
                    Barcode = item.Barcode ?? string.Empty,
                    Description = item.Description ?? string.Empty,
                    CategoryName = item.ProductCategory ?? string.Empty,
                    UnitPrice = item.Price,
                    CostPrice = item.CostPrice,
                    TaxRate = 0m,
                    QuantityOnHand = item.Quantity,
                    ReorderThreshold = item.ReorderThreshold,
                    AvailableOnPos = item.AvailableOnPos,
                    IsActive = item.ActiveProduct,
                    UpdatedAtUtc = item.UpdatedAt ?? DateTime.UtcNow
                });
            }

            if (products.Count >= paged.TotalCount || paged.Items.Count < pageSize) break;
            page++;
        }

        return products;
    }
}
