using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class CreateCustomerRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; init; } = string.Empty;

    [MaxLength(200)]
    [EmailAddress]
    public string? Email { get; init; }
}
