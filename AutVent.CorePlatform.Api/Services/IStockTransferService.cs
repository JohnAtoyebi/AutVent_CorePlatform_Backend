using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IStockTransferService
{
    Task<ApiResponse<StockTransferResponse>> CreateAsync(CreateStockTransferRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<StockTransferResponse>> GetByIdAsync(long id, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<StockTransferResponse>>> GetAllAsync(PagedQueryRequest request, long userId, CancellationToken cancellationToken = default);
}
