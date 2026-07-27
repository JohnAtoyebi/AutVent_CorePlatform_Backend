using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IProductCategoryService
{
    Task<ApiResponse<PagedResponse<CategoryResponse>>> GetAllAsync(PagedQueryRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IList<CategoryResponse>>> CreateBatchAsync(CreateCategoriesRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(long id, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IList<long>>> DeleteBatchAsync(IList<long> ids, long userId, CancellationToken cancellationToken = default);
}
