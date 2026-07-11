using AutVent.CorePlatform.Domain.Enums;

namespace AutVent.CorePlatform.Api.Services;

public interface IImageService
{
    Task<ImageUploadResult> UploadAsync(IFormFile file, long userId, ImageType imageType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string publicId, CancellationToken cancellationToken = default);
}

public sealed record ImageUploadResult(string Url, string PublicId);
