using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IMetricsService
{
    Task<ApiResponse<MetricsResponse>> GetAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<SalesSummaryResponse>> GetSalesSummaryAsync(SalesSummaryRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<SalesGraphResponse>> GetSalesGraphAsync(SalesSummaryRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<SaleResponse>>> GetRecentTransactionsAsync(SalesSummaryRequest request, PagedQueryRequest paging, long userId, CancellationToken cancellationToken = default);
}
