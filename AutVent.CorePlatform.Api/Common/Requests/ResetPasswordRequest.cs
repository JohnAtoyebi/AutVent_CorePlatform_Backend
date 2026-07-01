using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class ResetPasswordRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string EmailAddress { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Password must contain uppercase, lowercase, and a number")]
    public string NewPassword { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    [Compare(nameof(NewPassword), ErrorMessage = "New password and confirm password do not match")]
    public string ConfirmPassword { get; init; } = string.Empty;
}
