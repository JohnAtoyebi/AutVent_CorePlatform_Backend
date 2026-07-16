using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface ISupplierService
{
    Task<ApiResponse<SupplierResponse>> CreateAsync(CreateSupplierRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<SupplierResponse>> GetByIdAsync(long id, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<SupplierResponse>>> GetAllAsync(PagedQueryRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<SupplierResponse>> UpdateAsync(long id, UpdateSupplierRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(long id, long userId, CancellationToken cancellationToken = default);
}
