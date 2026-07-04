using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface ICustomerService
{
    Task<ApiResponse<CustomerResponse>> CreateAsync(CreateCustomerRequest request, long userId, long storeId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CustomerResponse>> GetByIdAsync(long id, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<CustomerResponse>>> GetAllAsync(PagedQueryRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CustomerResponse>> UpdateAsync(long id, CreateCustomerRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(long id, long userId, CancellationToken cancellationToken = default);
}
