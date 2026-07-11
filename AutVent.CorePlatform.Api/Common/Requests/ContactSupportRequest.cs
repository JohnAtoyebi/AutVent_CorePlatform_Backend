using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class ContactSupportRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Message { get; init; } = string.Empty;
}
