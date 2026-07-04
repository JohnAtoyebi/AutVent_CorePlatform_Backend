using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/business-industries")]
[Authorize]
public class BusinessIndustryController(IBusinessIndustryService businessIndustryService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CategoryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await businessIndustryService.GetAllAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("batch")]
    [ProducesResponseType(typeof(ApiResponse<IList<CategoryResponse>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<IList<CategoryResponse>>), StatusCodes.Status207MultiStatus)]
    public async Task<IActionResult> CreateBatch([FromBody] CreateCategoriesRequest request, CancellationToken cancellationToken)
    {
        var response = await businessIndustryService.CreateBatchAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<long>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<long>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] long id, CancellationToken cancellationToken)
    {
        var response = await businessIndustryService.DeleteAsync(id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
