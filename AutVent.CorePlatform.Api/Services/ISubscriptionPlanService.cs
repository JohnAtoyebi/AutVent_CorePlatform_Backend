using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface ISubscriptionPlanService
{
    Task<ApiResponse<PagedResponse<SubscriptionPlanResponse>>> GetAllAsync(PagedQueryRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<SubscriptionPlanResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}
