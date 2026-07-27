using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IWaitlistService
{
    Task<ApiResponse<WaitlistResponse>> JoinAsync(JoinWaitlistRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<WaitlistResponse>>> GetAllAsync(PagedQueryRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> MarkContactedAsync(long id, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
