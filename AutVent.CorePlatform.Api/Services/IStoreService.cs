using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IStoreService
{
    Task<ApiResponse<CreateStoreResponse>> CreateAsync(CreateStoreRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<CreateStoreResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyCollection<CreateStoreResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
}
