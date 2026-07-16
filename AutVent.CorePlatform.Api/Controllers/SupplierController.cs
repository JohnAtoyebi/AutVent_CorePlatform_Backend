using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class SupplierController(ISupplierService supplierService) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var response = await supplierService.CreateAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var response = await supplierService.GetByIdAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<SupplierResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await supplierService.GetAllAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        var response = await supplierService.UpdateAsync(id, request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var response = await supplierService.DeleteAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
