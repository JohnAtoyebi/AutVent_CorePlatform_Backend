using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Route("api/product-categories")]
[Authorize]
public class ProductCategoryController(IProductCategoryService productCategoryService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CategoryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await productCategoryService.GetAllAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("batch")]
    [ProducesResponseType(typeof(ApiResponse<IList<CategoryResponse>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<IList<CategoryResponse>>), StatusCodes.Status207MultiStatus)]
    public async Task<IActionResult> CreateBatch([FromBody] CreateCategoriesRequest request, CancellationToken cancellationToken)
    {
        var response = await productCategoryService.CreateBatchAsync(request, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete([FromRoute] long id, CancellationToken cancellationToken)
    {
        var response = await productCategoryService.DeleteAsync(id, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("batch")]
    [ProducesResponseType(typeof(ApiResponse<IList<long>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IList<long>>), StatusCodes.Status207MultiStatus)]
    [ProducesResponseType(typeof(ApiResponse<IList<long>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteBatch([FromBody] IList<long> ids, CancellationToken cancellationToken)
    {
        var response = await productCategoryService.DeleteBatchAsync(ids, CurrentUserId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
