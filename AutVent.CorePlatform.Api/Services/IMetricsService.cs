using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IMetricsService
{
    Task<ApiResponse<MetricsResponse>> GetAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<SalesSummaryResponse>> GetSalesSummaryAsync(SalesSummaryRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<SalesGraphResponse>> GetSalesGraphAsync(SalesSummaryRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<SaleResponse>>> GetRecentTransactionsAsync(SalesSummaryRequest request, PagedQueryRequest paging, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<SaleResponse>> GetTransactionByIdAsync(long id, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductMetricsResponse>> GetProductMetricsAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<InventoryMetricsResponse>> GetInventoryMetricsAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CustomerMetricsResponse>> GetCustomerMetricsAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<SalesByLocationResponse>> GetSalesByLocationAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<SalesByCategoryResponse>> GetSalesByCategoryAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PaymentMethodBreakdownResponse>> GetPaymentMethodBreakdownAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<TopCustomersResponse>> GetTopCustomersAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CustomerGrowthResponse>> GetCustomerGrowthAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<StaffAnalyticsResponse>> GetStaffAnalyticsAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductPerformanceResponse>> GetProductPerformanceAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<FinancialMetricsResponse>> GetFinancialMetricsAsync(MetricsRequest request, long userId, CancellationToken cancellationToken = default);
}
