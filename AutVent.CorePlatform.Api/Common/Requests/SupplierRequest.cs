using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class CreateSupplierRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [EmailAddress]
    [MaxLength(200)]
    public string? ContactEmail { get; init; }

    [Phone]
    [MaxLength(20)]
    public string? ContactPhone { get; init; }
}

public sealed class UpdateSupplierRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [EmailAddress]
    [MaxLength(200)]
    public string? ContactEmail { get; init; }

    [Phone]
    [MaxLength(20)]
    public string? ContactPhone { get; init; }
}
