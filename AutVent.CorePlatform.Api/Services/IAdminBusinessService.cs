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

    /// <summary>
    /// Get comprehensive business summary for dashboard with metrics on businesses and stores
    /// </summary>
    Task<ApiResponse<BusinessSummaryDashboardResponse>> GetBusinessSummaryDashboardAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user statistics for dashboard with optional date range filtering
    /// </summary>
    Task<ApiResponse<UserStatisticsDashboardResponse>> GetUserStatisticsDashboardAsync(DashboardAnalyticsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get MRR analytics for dashboard with plan breakdown and optional date range filtering
    /// </summary>
    Task<ApiResponse<MrrAnalyticsDashboardResponse>> GetMrrAnalyticsDashboardAsync(DashboardAnalyticsRequest request, CancellationToken cancellationToken = default);
}
