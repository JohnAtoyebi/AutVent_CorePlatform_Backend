using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class CustomerController(ICustomerService customerService) : ApiControllerBase
{
    [HttpPost("store/{storeId:long}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(long storeId, [FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var response = await customerService.CreateAsync(request, CurrentUserId, storeId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var response = await customerService.GetByIdAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CustomerResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await customerService.GetAllAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(long id, [FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var response = await customerService.UpdateAsync(id, request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var response = await customerService.DeleteAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
