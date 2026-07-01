using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class CreateProductRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Price { get; init; } = string.Empty;

    [Range(0, long.MaxValue)]
    public long Quantity { get; init; }

    [Required]
    [MaxLength(200)]
    public string ProductCategory { get; init; } = string.Empty;
}
