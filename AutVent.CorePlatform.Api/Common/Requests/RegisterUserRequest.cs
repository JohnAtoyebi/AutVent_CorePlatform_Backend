using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class RegisterUserRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string EmailAddress { get; init; } = string.Empty;

    [Required]
    [Phone]
    [MaxLength(20)]
    public string PhoneNumber { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).+$", ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character")]
    public string Password { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).+$", ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character")]
    [Compare(nameof(Password), ErrorMessage = "Password and confirm password do not match")]
    public string ConfirmPassword { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? ReferralCode { get; init; }
}
