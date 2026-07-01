using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BusinessController(IBusinessService businessService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateBusinessResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CreateBusinessResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<CreateBusinessResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateBusinessRequest request, CancellationToken cancellationToken)
    {
        var response = await businessService.CreateAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CreateBusinessResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CreateBusinessResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var response = await businessService.GetByIdAsync(id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<CreateBusinessResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var response = await businessService.GetAllAsync(cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
