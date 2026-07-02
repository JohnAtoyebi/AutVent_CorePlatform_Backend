using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class CreateBusinessRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Industry { get; init; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string StaffRange { get; init; } = string.Empty;
}
