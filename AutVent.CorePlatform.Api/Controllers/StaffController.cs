using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class StaffController(IStaffService staffService) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<StaffResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<StaffResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<StaffResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<StaffResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateStaffRequest request, CancellationToken cancellationToken)
    {
        var response = await staffService.CreateAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<StaffResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StaffResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var response = await staffService.GetByIdAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<StaffResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await staffService.GetAllAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<StaffResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StaffResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<StaffResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<StaffResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateStaffRequest request, CancellationToken cancellationToken)
    {
        var response = await staffService.UpdateAsync(id, request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPatch("{id:long}/role/{roleId:long}")]
    [ProducesResponseType(typeof(ApiResponse<StaffResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StaffResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeRole(long id, long roleId, CancellationToken cancellationToken)
    {
        var response = await staffService.ChangeRoleAsync(id, roleId, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPatch("{id:long}/status")]
    [ProducesResponseType(typeof(ApiResponse<StaffResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StaffResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(long id, [FromQuery] bool isActive, CancellationToken cancellationToken)
    {
        var response = await staffService.UpdateStatusAsync(id, isActive, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var response = await staffService.DeleteAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("roles")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<RoleResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var response = await staffService.GetRolesAsync(cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("roles/{roleId:long}")]
    [ProducesResponseType(typeof(ApiResponse<RoleDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RoleDetailResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleById(long roleId, CancellationToken cancellationToken)
    {
        var response = await staffService.GetRoleByIdAsync(roleId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("roles/{roleId:long}/permissions")]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRolePermissions(long roleId, [FromBody] UpdateRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var response = await staffService.UpdateRolePermissionsAsync(roleId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPatch("roles/{roleId:long}/permissions/{permissionId:long}/toggle")]
    [ProducesResponseType(typeof(ApiResponse<RoleDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RoleDetailResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<RoleDetailResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleRolePermission(long roleId, long permissionId, [FromQuery] bool isActive, CancellationToken cancellationToken)
    {
        var response = await staffService.ToggleRolePermissionAsync(roleId, permissionId, isActive, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
