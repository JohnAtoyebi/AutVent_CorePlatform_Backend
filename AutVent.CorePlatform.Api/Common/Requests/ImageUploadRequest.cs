using AutVent.CorePlatform.Domain.Enums;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class ImageUploadRequest
{
    public IFormFile File { get; init; } = null!;
    public ImageType ImageType { get; init; }
}
