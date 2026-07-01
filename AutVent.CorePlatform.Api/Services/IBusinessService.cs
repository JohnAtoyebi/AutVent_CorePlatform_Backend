using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IBusinessService
{
    Task<ApiResponse<CreateBusinessResponse>> CreateAsync(CreateBusinessRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<CreateBusinessResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyCollection<CreateBusinessResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
}
