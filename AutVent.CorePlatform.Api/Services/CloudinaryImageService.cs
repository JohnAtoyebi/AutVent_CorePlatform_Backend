using System.Text.RegularExpressions;
using AutVent.CorePlatform.Api.Common.Cloudinary;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AutVent.CorePlatform.Api.Services;

public sealed class CloudinaryImageService : IImageService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/svg+xml",
        "image/bmp"
    };

    private readonly Cloudinary _cloudinary;
    private readonly IUnitOfWork _unitOfWork;

    public CloudinaryImageService(IOptions<CloudinaryOptions> options, IUnitOfWork unitOfWork)
    {
        var opt = options.Value;
        var account = new Account(opt.CloudName, opt.ApiKey, opt.ApiSecret);
        _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
        _unitOfWork = unitOfWork;
    }

    public async Task<ImageUploadResult> UploadAsync(IFormFile file, long userId, ImageType imageType, CancellationToken cancellationToken = default)
    {
        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new InvalidOperationException(
                $"Invalid file type '{file.ContentType}'. Only image files (JPEG, PNG, GIF, WEBP, SVG, BMP) are allowed.");

        var business = await _unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("No business found for the current user.");

        var businessSlug = Slugify(business.BusinessName);
        var folder = $"{businessSlug}/{imageType}";

        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.Error is not null)
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");

        return new ImageUploadResult(result.SecureUrl.ToString(), result.PublicId);
    }

    public async Task DeleteAsync(string publicId, CancellationToken cancellationToken = default)
    {
        var deleteParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);

        if (result.Error is not null)
            throw new InvalidOperationException($"Cloudinary delete failed: {result.Error.Message}");
    }

    private static string Slugify(string value)
        => Regex.Replace(value.Trim().ToUpperInvariant(), @"[^A-Z0-9]+", "-").Trim('-');
}
