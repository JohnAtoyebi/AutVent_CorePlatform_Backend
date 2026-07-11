using System.ComponentModel.DataAnnotations;
using AutVent.CorePlatform.Domain.Enums;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class BulkEditProductRequest
{
    [Required]
    [MinLength(1)]
    public List<long> ProductIds { get; init; } = [];

    public long? ProductCategoryId { get; init; }

    public bool? IsActive { get; init; }

    public bool? AvailableOnPos { get; init; }

    [MaxLength(200)]
    public string? Supplier { get; init; }

    public PriceAdjustmentRequest? PriceAdjustment { get; init; }
}

public sealed class PriceAdjustmentRequest
{
    [Required]
    [EnumDataType(typeof(PriceAdjustmentType))]
    public PriceAdjustmentType Type { get; init; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Value { get; init; }
}
