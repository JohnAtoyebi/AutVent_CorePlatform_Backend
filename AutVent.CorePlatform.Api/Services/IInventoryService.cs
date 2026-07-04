using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IInventoryService
{
    Task<ApiResponse<InventorySummaryResponse>> GetSummaryAsync(InventorySummaryFilterRequest request, long userId, long storeId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<InventoryItemResponse>>> GetItemsAsync(PagedQueryRequest request, long userId, long storeId, CancellationToken cancellationToken = default);
    Task<ApiResponse<InventoryItemResponse>> UpdateStockAsync(long productId, UpdateInventoryStockRequest request, long userId, long storeId, CancellationToken cancellationToken = default);
}
