using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IAdminUserService
{
    Task<ApiResponse<UserOverviewResponse>> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<UserProfileResponse>>> GetAllAsync(PagedQueryRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> ActivateAsync(long id, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeactivateAsync(long id, long userId, CancellationToken cancellationToken = default);
}
