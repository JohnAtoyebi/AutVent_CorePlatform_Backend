using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IStaffRangeService
{
    Task<ApiResponse<PagedResponse<CategoryResponse>>> GetAllAsync(PagedQueryRequest request, CancellationToken cancellationToken = default);
}
