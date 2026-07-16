using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IBillingService
{
    Task<ApiResponse<BillingTransactionResponse>> CreateAsync(long businessId, CreateBillingTransactionRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<BillingTransactionResponse>> VerifyAsync(VerifyBillingTransactionRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<BillingTransactionResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<BillingTransactionResponse>>> GetAllAsync(long businessId, PagedQueryRequest request, CancellationToken cancellationToken = default);
}
