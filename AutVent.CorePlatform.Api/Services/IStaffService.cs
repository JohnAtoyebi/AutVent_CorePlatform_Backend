using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IStaffService
{
    Task<ApiResponse<StaffResponse>> CreateAsync(CreateStaffRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<StaffResponse>> GetByIdAsync(long id, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<StaffResponse>>> GetAllAsync(PagedQueryRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<StaffResponse>> UpdateAsync(long id, UpdateStaffRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<StaffResponse>> ChangeRoleAsync(long id, long roleId, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(long id, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyCollection<RoleResponse>>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<RoleDetailResponse>> GetRoleByIdAsync(long roleId, CancellationToken cancellationToken = default);
    Task<ApiResponse<RoleDetailResponse>> ToggleRolePermissionAsync(long roleId, long permissionId, bool isActive, CancellationToken cancellationToken = default);
    Task<ApiResponse<RoleResponse>> UpdateRolePermissionsAsync(long roleId, UpdateRolePermissionsRequest request, CancellationToken cancellationToken = default);
}
