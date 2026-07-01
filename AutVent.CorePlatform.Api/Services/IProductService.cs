using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IProductService
{
    Task<ApiResponse<IReadOnlyCollection<ProductResponse>>> CreateAsync(IReadOnlyCollection<CreateProductRequest> requests, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyCollection<ProductResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
}
