namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class StaffResponse
{
    public long StaffId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string EmailAddress { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public long RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public bool HasAccessToAllStores { get; init; }
    public List<StaffStoreAccessResponse> StoreAccess { get; init; } = [];
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class StaffStoreAccessResponse
{
    public long StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
}

public sealed class RoleResponse
{
    public long RoleId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsDefault { get; init; }
    public List<PermissionResponse> Permissions { get; init; } = [];
}

public sealed class RoleDetailResponse
{
    public long RoleId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsDefault { get; init; }
    public List<PermissionResponse> Permissions { get; init; } = [];
    public List<RoleMemberResponse> Members { get; init; } = [];
}

public sealed class RoleMemberResponse
{
    public long StaffId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string EmailAddress { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public bool IsActive { get; init; }
}

public sealed class PermissionResponse
{
    public long PermissionId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Group { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
