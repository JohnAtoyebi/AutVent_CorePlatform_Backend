using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface ICategoryService
{
    Task<ApiResponse<PagedResponse<CategoryResponse>>> GetBusinessIndustriesAsync(PagedQueryRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<CategoryResponse>> CreateBusinessIndustryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<CategoryResponse>>> GetProductCategoriesAsync(PagedQueryRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<CategoryResponse>> CreateProductCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<CategoryResponse>>> GetStoreCategoriesAsync(PagedQueryRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<CategoryResponse>> CreateStoreCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
}
