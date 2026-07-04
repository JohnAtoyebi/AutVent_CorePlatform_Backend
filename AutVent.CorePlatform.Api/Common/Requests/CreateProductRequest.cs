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

    [Range(1, long.MaxValue)]
    public long Quantity { get; init; }

    [Required]
    public long ProductCategoryId { get; init; }
}
