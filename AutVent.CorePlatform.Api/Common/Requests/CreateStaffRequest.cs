using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class CreateStaffRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string EmailAddress { get; init; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; init; }

    [Required]
    public long RoleId { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }

    public bool HasAccessToAllStores { get; init; } = false;

    public List<long>? StoreIds { get; init; }
}

public sealed class UpdateStaffRequest
{
    [MaxLength(200)]
    public string? FullName { get; init; }

    [EmailAddress]
    [MaxLength(200)]
    public string? EmailAddress { get; init; }

    [MaxLength(20)]
    public string? PhoneNumber { get; init; }

    public long? RoleId { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }

    public bool? HasAccessToAllStores { get; init; }

    public List<long>? StoreIds { get; init; }
}

public sealed class UpdateRolePermissionsRequest
{
    /// <summary>
    /// The full list of permission IDs to assign to this role.
    /// Existing permissions not in this list will be removed.
    /// </summary>
    [Required]
    public List<long> PermissionIds { get; init; } = [];
}
