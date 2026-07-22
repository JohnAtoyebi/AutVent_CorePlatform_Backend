using AutVent.Application.Contracts;
using AutVent.Domain.Entities;

namespace AutVent.Application.Abstractions.Api;

public interface IInventoryApiClient
{
    /// <summary>GET /api/Inventory/store/{storeId}/items — server uses long storeId.</summary>
    Task<IReadOnlyList<InventoryRecord>> GetInventoryAsync(long remoteStoreId, Guid localStoreId, CancellationToken cancellationToken);

    /// <summary>POST /api/Inventory/store/{storeId}/product/{productId}/stock</summary>
    Task UploadAdjustmentAsync(AdjustInventoryCommand command, string deduplicationKey, CancellationToken cancellationToken);
}
