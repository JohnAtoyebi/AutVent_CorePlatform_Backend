using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IAdminBusinessService
{
    Task<ApiResponse<bool>> ActivateAsync(long id, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeactivateAsync(long id, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<BusinessOverviewResponse>> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<BusinessStoreResponse>>> GetStoresAsync(long businessId, PagedQueryRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<BusinessProductResponse>>> GetProductsAsync(long businessId, PagedQueryRequest request, long userId, CancellationToken cancellationToken = default);
}
