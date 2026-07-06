using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IStoreService
{
    Task<ApiResponse<CreateStoreResponse>> CreateAsync(CreateStoreRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CreateStoreResponse>> GetByIdAsync(long id, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<CreateStoreResponse>>> GetAllAsync(PagedQueryRequest request, long userId, CancellationToken cancellationToken = default);
}
