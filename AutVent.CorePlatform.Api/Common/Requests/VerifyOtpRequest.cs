using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class VerifyOtpRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string EmailAddress { get; init; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; init; } = string.Empty;
}
