using AutVent.Domain.Entities;

namespace AutVent.Application.Abstractions.Api;

public interface ICatalogApiClient
{
    /// <summary>Fetches all stores from GET /api/Store (paginated, walks all pages).</summary>
    Task<IReadOnlyList<Store>> GetStoresAsync(CancellationToken cancellationToken);

    /// <summary>Fetches all products for a store from GET /api/Product/store/{storeId}.</summary>
    Task<IReadOnlyList<Product>> GetProductsAsync(long remoteStoreId, CancellationToken cancellationToken);
}
