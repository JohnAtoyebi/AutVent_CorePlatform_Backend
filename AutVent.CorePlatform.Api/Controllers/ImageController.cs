using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutVent.CorePlatform.Api.Controllers;

[Authorize]
[Route("api/images")]
public sealed class ImageController(IImageService imageService) : ApiControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<ImageUploadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ImageUploadResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(
        [FromForm] ImageUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await imageService.UploadAsync(request.File, CurrentUserId, request.ImageType, cancellationToken);
            return Ok(ApiResponse<ImageUploadResponse>.Ok(new ImageUploadResponse(result.Url, result.PublicId)));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ImageUploadResponse>.Failed(
                StatusCodes.Status400BadRequest,
                ex.Message,
                [new ApiError("InvalidFile", ex.Message, nameof(request.File))]));
        }
    }

    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(
        [FromQuery] string publicId,
        CancellationToken cancellationToken = default)
    {
        await imageService.DeleteAsync(publicId, cancellationToken);

        return Ok(ApiResponse<string>.Ok("Image deleted successfully."));
    }
}
