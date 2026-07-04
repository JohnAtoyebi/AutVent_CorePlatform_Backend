using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IPosService
{
    Task<ApiResponse<SaleResponse>> CreateSaleAsync(CreateSaleRequest request, long userId, long storeId, CancellationToken cancellationToken = default);
    Task<ApiResponse<SaleResponse>> GetSaleByIdAsync(long id, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<SaleResponse>>> GetSalesByStoreAsync(PagedQueryRequest request, long userId, long storeId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<SaleResponse>>> GetAllSalesAsync(PagedQueryRequest request, long userId, CancellationToken cancellationToken = default);
}
