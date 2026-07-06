using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IProductService
{
    Task<ApiResponse<IReadOnlyCollection<ProductResponse>>> CreateAsync(IReadOnlyCollection<CreateProductRequest> requests, long userId, long storeId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyCollection<ProductResponse>>> UpdateAsync(long id, CreateProductRequest request, long userId, long storeId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductImportResponse>> ImportAsync(IFormFile file, long userId, long storeId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductResponse>> GetByIdAsync(long id, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<ProductResponse>>> GetAllAsync(PagedQueryRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(long id, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> UpdateStatusAsync(long id, bool isActive, long userId, CancellationToken cancellationToken = default);
}
