using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class ChangeEmailRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string NewEmailAddress { get; init; } = string.Empty;

    [Required]
    public string CurrentPassword { get; init; } = string.Empty;
}
