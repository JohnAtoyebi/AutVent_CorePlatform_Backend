using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class CreateBusinessRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required]
    public int IndustryId { get; init; }

    [Required]
    public int StaffRangeId { get; init; }

    [MaxLength(1000)]
    public string? LogoUrl { get; init; }

    [EmailAddress]
    [MaxLength(200)]
    public string? Email { get; init; }

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; init; }

    [MaxLength(500)]
    public string? Website { get; init; }

    [MaxLength(500)]
    public string? Address { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(100)]
    public string? State { get; init; }

    [MaxLength(100)]
    public string? Country { get; init; }
}
