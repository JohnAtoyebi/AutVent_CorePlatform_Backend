using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class JoinWaitlistRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string EmailAddress { get; init; } = string.Empty;

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; init; }

    [MaxLength(200)]
    public string? BusinessType { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }
}
