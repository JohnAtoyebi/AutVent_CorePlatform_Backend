using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class UpdateStoreRequest
{
    [MaxLength(200)]
    public string? Name { get; init; }

    public int? StoreCategoryId { get; init; }

    [EmailAddress]
    [MaxLength(200)]
    public string? EmailAddress { get; init; }

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; init; }

    [MaxLength(500)]
    public string? Address { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(100)]
    public string? State { get; init; }

    [MaxLength(100)]
    public string? Country { get; init; }
}
