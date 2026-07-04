using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IProductCategoryService
{
    Task<ApiResponse<PagedResponse<CategoryResponse>>> GetAllAsync(PagedQueryRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<IList<CategoryResponse>>> CreateBatchAsync(CreateCategoriesRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
