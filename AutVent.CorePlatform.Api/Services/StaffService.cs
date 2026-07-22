using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class StaffService(IUnitOfWork unitOfWork, IAuditLogService auditLogService) : IStaffService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<StaffResponse>> CreateAsync(CreateStaffRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var business = await unitOfWork.Query<Business>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (business is null)
        {
            return ApiResponse<StaffResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Business not found for this user",
                [new ApiError("BusinessNotFound", "Create a business before adding staff", nameof(userId))]);
        }

        var role = await unitOfWork.Query<Role>()
            .FirstOrDefaultAsync(x => x.Id == request.RoleId, cancellationToken);

        if (role is null)
        {
            return ApiResponse<StaffResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Role not found",
                [new ApiError("RoleNotFound", "No role found for the provided id", nameof(request.RoleId))]);
        }

        var emailExists = await unitOfWork.Query<Staff>()
            .AnyAsync(x => x.EmailAddress.ToLower() == request.EmailAddress.ToLower() && x.BusinessId == business.Id && !x.IsDeleted, cancellationToken);

        if (emailExists)
        {
            return ApiResponse<StaffResponse>.Failed(
                StatusCodes.Status409Conflict,
                "A staff member with this email already exists",
                [new ApiError("DuplicateEmail", "Email address is already in use within this business", nameof(request.EmailAddress))]);
        }

        // Build store access list
        List<StaffStoreAccess> storeAccessList = new();

        if (request.HasAccessToAllStores)
        {
            var allStoreIds = await unitOfWork.Query<Store>()
                .Where(x => x.BusinessId == business.Id && !x.IsDeleted)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            storeAccessList = allStoreIds.Select(storeId => new StaffStoreAccess
            {
                StoreId = storeId,
                IsActive = true,
                CreatedBy = SystemActor,
                DateCreated = DateTime.UtcNow
            }).ToList();
        }
        else if (request.StoreIds is { Count: > 0 })
        {
            var validStores = await unitOfWork.Query<Store>()
                .Where(x => request.StoreIds.Contains(x.Id) && x.BusinessId == business.Id)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            var invalidStoreIds = request.StoreIds.Except(validStores).ToList();
            if (invalidStoreIds.Count > 0)
            {
                return ApiResponse<StaffResponse>.Failed(
                    StatusCodes.Status404NotFound,
                    "One or more store IDs not found in your business",
                    invalidStoreIds.Select(id => new ApiError("StoreNotFound", $"Store {id} does not belong to your business", "storeIds")));
            }

            storeAccessList = validStores.Select(storeId => new StaffStoreAccess
            {
                StoreId = storeId,
                IsActive = true,
                CreatedBy = SystemActor,
                DateCreated = DateTime.UtcNow
            }).ToList();
        }

        var now = DateTime.UtcNow;
        var staff = new Staff
        {
            FullName = request.FullName.Trim(),
            EmailAddress = request.EmailAddress.Trim().ToLower(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            BusinessId = business.Id,
            RoleId = role.Id,
            HasAccessToAllStores = request.HasAccessToAllStores,
            Notes = request.Notes,
            StoreAccess = storeAccessList,
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = now
        };

        await unitOfWork.CreateAsync(staff, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await LoadStaffWithIncludes(staff.Id, cancellationToken);
        return ApiResponse<StaffResponse>.Created(MapToResponse(created!), "Staff member created successfully");
    }

    public async Task<ApiResponse<StaffResponse>> GetByIdAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var staff = await LoadStaffWithIncludes(id, cancellationToken);

        if (staff is null || staff.Business.UserId != userId || staff.IsDeleted)
        {
            return ApiResponse<StaffResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Staff member not found",
                [new ApiError("StaffNotFound", "No staff member found for this id", nameof(id))]);
        }

        return ApiResponse<StaffResponse>.Ok(MapToResponse(staff));
    }

    public async Task<ApiResponse<PagedResponse<StaffResponse>>> GetAllAsync(PagedQueryRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = unitOfWork.Query<Staff>()
            .Include(x => x.Business)
            .Include(x => x.Role)
            .Include(x => x.StoreAccess).ThenInclude(x => x.Store)
            .Where(x => x.Business.UserId == userId && !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.FullName.ToLower().Contains(search) ||
                x.EmailAddress.ToLower().Contains(search));
        }

        if (request.Filters is not null)
        {
            if (request.Filters.TryGetValue("roleId", out var roleFilter) && long.TryParse(roleFilter, out var roleId))
                query = query.Where(x => x.RoleId == roleId);

            if (request.Filters.TryGetValue("isActive", out var activeFilter) && bool.TryParse(activeFilter, out var isActive))
                query = query.Where(x => x.IsActive == isActive);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var paged = new PagedResponse<StaffResponse>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items.Select(MapToResponse).ToList()
        };

        return ApiResponse<PagedResponse<StaffResponse>>.Ok(paged);
    }

    public async Task<ApiResponse<StaffResponse>> UpdateAsync(long id, UpdateStaffRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var staff = await LoadStaffWithIncludes(id, cancellationToken);

        if (staff is null || staff.Business.UserId != userId || staff.IsDeleted)
        {
            return ApiResponse<StaffResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Staff member not found",
                [new ApiError("StaffNotFound", "No staff member found for this id", nameof(id))]);
        }

        var now = DateTime.UtcNow;

        if (request.RoleId.HasValue && request.RoleId.Value != staff.RoleId)
        {
            var role = await unitOfWork.Query<Role>()
                .FirstOrDefaultAsync(x => x.Id == request.RoleId.Value, cancellationToken);

            if (role is null)
            {
                return ApiResponse<StaffResponse>.Failed(
                    StatusCodes.Status404NotFound,
                    "Role not found",
                    [new ApiError("RoleNotFound", "No role found for the provided id", nameof(request.RoleId))]);
            }

            if (role.Name.Equals("Owner", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<StaffResponse>.Failed(
                    StatusCodes.Status403Forbidden,
                    "The Owner role cannot be assigned to a staff member. Use Transfer Ownership instead.",
                    [new ApiError("OwnerRoleReserved", "Owner role assignment is not permitted through this operation", nameof(request.RoleId))]);
            }

            if (staff.Role.Name.Equals("Owner", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<StaffResponse>.Failed(
                    StatusCodes.Status403Forbidden,
                    "The Owner's role cannot be changed. Use Transfer Ownership to reassign ownership.",
                    [new ApiError("OwnerRoleProtected", "Cannot change the role of the business owner", nameof(id))]);
            }

            staff.RoleId = role.Id;
        }

        if (request.EmailAddress is not null)
        {
            var normalizedEmail = request.EmailAddress.Trim().ToLower();
            if (normalizedEmail != staff.EmailAddress)
            {
                var emailExists = await unitOfWork.Query<Staff>()
                    .AnyAsync(x => x.EmailAddress.ToLower() == normalizedEmail && x.BusinessId == staff.BusinessId && x.Id != id && !x.IsDeleted, cancellationToken);

                if (emailExists)
                {
                    return ApiResponse<StaffResponse>.Failed(
                        StatusCodes.Status409Conflict,
                        "A staff member with this email already exists",
                        [new ApiError("DuplicateEmail", "Email address is already in use within this business", nameof(request.EmailAddress))]);
                }

                staff.EmailAddress = normalizedEmail;
            }
        }

        if (request.FullName is not null) staff.FullName = request.FullName.Trim();
        if (request.PhoneNumber is not null) staff.PhoneNumber = request.PhoneNumber.Trim();
        if (request.Notes is not null) staff.Notes = request.Notes;

        if (request.HasAccessToAllStores.HasValue)
        {
            staff.HasAccessToAllStores = request.HasAccessToAllStores.Value;
        }

        // Rebuild store access when HasAccessToAllStores is toggled or StoreIds are provided
        bool rebuildAccess = request.HasAccessToAllStores.HasValue || request.StoreIds is not null;
        if (rebuildAccess)
        {
            unitOfWork.DeleteRange(staff.StoreAccess.ToList());

            if (staff.HasAccessToAllStores)
            {
                var allStoreIds = await unitOfWork.Query<Store>()
                    .Where(x => x.BusinessId == staff.BusinessId && !x.IsDeleted)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

                var allAccess = allStoreIds.Select(storeId => new StaffStoreAccess
                {
                    StaffId = staff.Id,
                    StoreId = storeId,
                    IsActive = true,
                    CreatedBy = SystemActor,
                    DateCreated = now
                }).ToList();

                await unitOfWork.CreateRangeAsync(allAccess, cancellationToken);
            }
            else if (request.StoreIds is { Count: > 0 })
            {
                var validStores = await unitOfWork.Query<Store>()
                    .Where(x => request.StoreIds.Contains(x.Id) && x.BusinessId == staff.BusinessId)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

                var invalidIds = request.StoreIds.Except(validStores).ToList();
                if (invalidIds.Count > 0)
                {
                    return ApiResponse<StaffResponse>.Failed(
                        StatusCodes.Status404NotFound,
                        "One or more store IDs not found in your business",
                        invalidIds.Select(sid => new ApiError("StoreNotFound", $"Store {sid} does not belong to your business", "storeIds")));
                }

                var newAccess = validStores.Select(storeId => new StaffStoreAccess
                {
                    StaffId = staff.Id,
                    StoreId = storeId,
                    IsActive = true,
                    CreatedBy = SystemActor,
                    DateCreated = now
                }).ToList();

                await unitOfWork.CreateRangeAsync(newAccess, cancellationToken);
            }
        }

        staff.DateUpdated = now;
        staff.UpdatedBy = SystemActor;

        unitOfWork.Update(staff);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await LoadStaffWithIncludes(staff.Id, cancellationToken);
        return ApiResponse<StaffResponse>.Ok(MapToResponse(updated!), "Staff member updated successfully");
    }

    public async Task<ApiResponse<StaffResponse>> ChangeRoleAsync(long id, long roleId, long userId, CancellationToken cancellationToken = default)
    {
        var staff = await LoadStaffWithIncludes(id, cancellationToken);

        if (staff is null || staff.Business.UserId != userId || staff.IsDeleted)
        {
            return ApiResponse<StaffResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Staff member not found",
                [new ApiError("StaffNotFound", "No staff member found for this id", nameof(id))]);
        }

        if (staff.RoleId == roleId)
        {
            return ApiResponse<StaffResponse>.Ok(MapToResponse(staff), "Staff role is already set to the requested role");
        }

        var role = await unitOfWork.Query<Role>()
            .FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken);

        if (role is null)
        {
            return ApiResponse<StaffResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Role not found",
                [new ApiError("RoleNotFound", "No role found for the provided id", nameof(roleId))]);
        }

        // The Owner role is exclusive and cannot be assigned or taken away via this endpoint
        if (role.Name.Equals("Owner", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<StaffResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "The Owner role cannot be assigned to a staff member. Use Transfer Ownership instead.",
                [new ApiError("OwnerRoleReserved", "Owner role assignment is not permitted through this operation", nameof(roleId))]);
        }

        if (staff.Role.Name.Equals("Owner", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<StaffResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "The Owner's role cannot be changed. Use Transfer Ownership to reassign ownership.",
                [new ApiError("OwnerRoleProtected", "Cannot change the role of the business owner", nameof(id))]);
        }

        staff.RoleId = role.Id;
        staff.DateUpdated = DateTime.UtcNow;
        staff.UpdatedBy = SystemActor;

        unitOfWork.Update(staff);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await LoadStaffWithIncludes(staff.Id, cancellationToken);
        return ApiResponse<StaffResponse>.Ok(MapToResponse(updated!), "Staff role updated successfully");
    }

    public async Task<ApiResponse<StaffResponse>> UpdateStatusAsync(long id, bool isActive, long userId, CancellationToken cancellationToken = default)
    {
        var staff = await LoadStaffWithIncludes(id, cancellationToken);

        if (staff is null || staff.Business.UserId != userId || staff.IsDeleted)
        {
            return ApiResponse<StaffResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Staff member not found",
                [new ApiError("StaffNotFound", "No staff member found for this id", nameof(id))]);
        }

        if (staff.IsActive == isActive)
        {
            return ApiResponse<StaffResponse>.Ok(MapToResponse(staff),
                $"Staff member is already {(isActive ? "active" : "inactive")}");
        }

        staff.IsActive = isActive;
        staff.DateUpdated = DateTime.UtcNow;
        staff.UpdatedBy = SystemActor;

        unitOfWork.Update(staff);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!isActive)
        {
            await auditLogService.LogAsync(
                userId,
                AuditAction.StaffDeactivated,
                nameof(Staff),
                $"Staff member '{staff.FullName}' was deactivated.",
                businessId: staff.BusinessId,
                entityId: staff.Id,
                cancellationToken: cancellationToken);
        }

        var updated = await LoadStaffWithIncludes(staff.Id, cancellationToken);
        return ApiResponse<StaffResponse>.Ok(MapToResponse(updated!),
            $"Staff member {(isActive ? "activated" : "deactivated")} successfully");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var staff = await unitOfWork.Query<Staff>()
            .Include(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (staff is null || staff.Business.UserId != userId)
        {
            return ApiResponse<bool>.Failed(
                StatusCodes.Status404NotFound,
                "Staff member not found",
                [new ApiError("StaffNotFound", "No staff member found for this id", nameof(id))]);
        }

        var now = DateTime.UtcNow;
        staff.IsDeleted = true;
        staff.IsActive = false;
        staff.DateDeleted = now;
        staff.DateUpdated = now;
        staff.UpdatedBy = SystemActor;

        unitOfWork.Update(staff);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            userId,
            AuditAction.StaffDeleted,
            nameof(Staff),
            $"Staff member '{staff.FullName}' was deleted.",
            businessId: staff.BusinessId,
            entityId: staff.Id,
            cancellationToken: cancellationToken);

        return ApiResponse<bool>.Ok(true, "Staff member deleted successfully");
    }

    public async Task<ApiResponse<IReadOnlyCollection<RoleResponse>>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await unitOfWork.Query<Role>()
            .Include(x => x.RolePermissions).ThenInclude(x => x.Permission)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var result = roles.Select(x => new RoleResponse
        {
            RoleId = x.Id,
            Name = x.Name,
            Description = x.Description,
            IsDefault = x.IsDefault,
            Permissions = x.RolePermissions.Select(rp => new PermissionResponse
            {
                PermissionId = rp.Permission.Id,
                Name = rp.Permission.Name,
                Description = rp.Permission.Description,
                Group = rp.Permission.Group,
                IsActive = rp.IsActive
            }).ToList()
        }).ToList();

        return ApiResponse<IReadOnlyCollection<RoleResponse>>.Ok(result);
    }

    public async Task<ApiResponse<RoleDetailResponse>> GetRoleByIdAsync(long roleId, CancellationToken cancellationToken = default)
    {
        var role = await unitOfWork.Query<Role>()
            .Include(x => x.RolePermissions).ThenInclude(x => x.Permission)
            .Include(x => x.StaffMembers.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken);

        if (role is null)
        {
            return ApiResponse<RoleDetailResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Role not found",
                [new ApiError("RoleNotFound", "No role found for the provided id", nameof(roleId))]);
        }

        return ApiResponse<RoleDetailResponse>.Ok(new RoleDetailResponse
        {
            RoleId = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsDefault = role.IsDefault,
            Permissions = role.RolePermissions.Select(rp => new PermissionResponse
            {
                PermissionId = rp.Permission.Id,
                Name = rp.Permission.Name,
                Description = rp.Permission.Description,
                Group = rp.Permission.Group,
                IsActive = rp.IsActive
            }).ToList(),
            Members = role.StaffMembers.Select(s => new RoleMemberResponse
            {
                StaffId = s.Id,
                FullName = s.FullName,
                EmailAddress = s.EmailAddress,
                PhoneNumber = s.PhoneNumber,
                IsActive = s.IsActive
            }).ToList()
        });
    }

    public async Task<ApiResponse<RoleDetailResponse>> ToggleRolePermissionAsync(long roleId, long permissionId, bool isActive, CancellationToken cancellationToken = default)
    {
        var role = await unitOfWork.Query<Role>()
            .FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken);

        if (role is null)
        {
            return ApiResponse<RoleDetailResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Role not found",
                [new ApiError("RoleNotFound", "No role found for the provided id", nameof(roleId))]);
        }

        if (role.Name.Equals("Owner", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<RoleDetailResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "Owner permissions cannot be modified.",
                [new ApiError("OwnerPermissionsProtected", "The Owner role has fixed permissions and cannot be changed", nameof(roleId))]);
        }

        var rolePermission = await unitOfWork.Query<RolePermission>()
            .FirstOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permissionId, cancellationToken);

        if (rolePermission is null)
        {
            return ApiResponse<RoleDetailResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Permission is not assigned to this role",
                [new ApiError("PermissionNotAssigned", "The permission is not part of this role", nameof(permissionId))]);
        }

        rolePermission.IsActive = isActive;
        rolePermission.DateUpdated = DateTime.UtcNow;
        rolePermission.UpdatedBy = SystemActor;

        unitOfWork.Update(rolePermission);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetRoleByIdAsync(roleId, cancellationToken);
    }

    public async Task<ApiResponse<RoleResponse>> UpdateRolePermissionsAsync(long roleId, UpdateRolePermissionsRequest request, CancellationToken cancellationToken = default)
    {
        var role = await unitOfWork.Query<Role>()
            .Include(x => x.RolePermissions)
            .FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken);

        if (role is null)
        {
            return ApiResponse<RoleResponse>.Failed(
                StatusCodes.Status404NotFound,
                "Role not found",
                [new ApiError("RoleNotFound", "No role found for the provided id", nameof(roleId))]);
        }

        if (role.Name.Equals("Owner", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<RoleResponse>.Failed(
                StatusCodes.Status403Forbidden,
                "Owner permissions cannot be modified.",
                [new ApiError("OwnerPermissionsProtected", "The Owner role has fixed permissions and cannot be changed", nameof(roleId))]);
        }

        // Validate all provided permission IDs exist
        var validPermissionIds = await unitOfWork.Query<Permission>()
            .Where(x => request.PermissionIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var invalidIds = request.PermissionIds.Except(validPermissionIds).ToList();
        if (invalidIds.Count > 0)
        {
            return ApiResponse<RoleResponse>.Failed(
                StatusCodes.Status404NotFound,
                "One or more permission IDs are invalid",
                invalidIds.Select(pid => new ApiError("PermissionNotFound", $"Permission {pid} does not exist", "permissionIds")));
        }

        // Replace permissions: remove all existing, add the new set
        var existing = role.RolePermissions.ToList();
        unitOfWork.DeleteRange(existing);

        var now = DateTime.UtcNow;
        var newPermissions = validPermissionIds.Select(pid => new RolePermission
        {
            RoleId = role.Id,
            PermissionId = pid,
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = now
        }).ToList();

        await unitOfWork.CreateRangeAsync(newPermissions, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with updated permissions
        var updated = await unitOfWork.Query<Role>()
            .Include(x => x.RolePermissions).ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken);

        return ApiResponse<RoleResponse>.Ok(new RoleResponse
        {
            RoleId = updated!.Id,
            Name = updated.Name,
            Description = updated.Description,
            IsDefault = updated.IsDefault,
            Permissions = updated.RolePermissions.Select(rp => new PermissionResponse
            {
                PermissionId = rp.Permission.Id,
                Name = rp.Permission.Name,
                Description = rp.Permission.Description,
                Group = rp.Permission.Group,
                IsActive = rp.IsActive
            }).ToList()
        }, "Role permissions updated successfully");
    }

    private Task<Staff?> LoadStaffWithIncludes(long id, CancellationToken cancellationToken) =>
        unitOfWork.Query<Staff>()
            .Include(x => x.Business)
            .Include(x => x.Role)
            .Include(x => x.StoreAccess).ThenInclude(x => x.Store)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    private static StaffResponse MapToResponse(Staff staff) => new()
    {
        StaffId = staff.Id,
        FullName = staff.FullName,
        EmailAddress = staff.EmailAddress,
        PhoneNumber = staff.PhoneNumber,
        RoleId = staff.RoleId,
        RoleName = staff.Role.Name,
        HasAccessToAllStores = staff.HasAccessToAllStores,
        StoreAccess = staff.StoreAccess.Select(x => new StaffStoreAccessResponse
        {
            StoreId = x.StoreId,
            StoreName = x.Store.Name
        }).ToList(),
        Notes = staff.Notes,
        IsActive = staff.IsActive,
        CreatedAt = staff.DateCreated
    };
}
