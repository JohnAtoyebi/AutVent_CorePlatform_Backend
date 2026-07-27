using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IBusinessService
{
    Task<ApiResponse<CreateBusinessResponse>> CreateAsync(CreateBusinessRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CreateBusinessResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ApiResponse<CreateBusinessResponse>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<CreateBusinessResponse>>> GetAllAsync(PagedQueryRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<CreateBusinessResponse>> UpdateAsync(long id, UpdateBusinessRequest request, long userId, CancellationToken cancellationToken = default);
}
