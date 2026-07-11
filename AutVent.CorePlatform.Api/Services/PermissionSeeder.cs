using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class PermissionSeeder(IUnitOfWork unitOfWork)
{
    private const string SystemActor = "system";

    // (Name, Description, Group)
    private static readonly (string Name, string Description, string Group)[] DefaultPermissions =
    [
        // Dashboard
        ("dashboard.view",              "View dashboard",                                   "Dashboard"),

        // Reports
        ("reports.view",                "View reports and analytics",                       "Reports"),
        ("reports.export",              "Export reports",                                   "Reports"),

        // Inventory
        ("inventory.view",              "View inventory",                                   "Inventory"),
        ("inventory.adjust",            "Adjust stock levels",                              "Inventory"),
        ("inventory.transfer",          "Transfer stock between stores",                    "Inventory"),
        ("inventory.manage_products",   "Manage products",                                  "Inventory"),

        // POS / Sales
        ("pos.access",                  "Access point of sale",                             "POS"),
        ("pos.refunds",                 "Process refunds",                                  "POS"),
        ("pos.sales_history",           "View sales history",                               "POS"),

        // Staff
        ("staff.view",                  "View staff list",                                  "Staff"),
        ("staff.manage",                "Add and edit staff members",                       "Staff"),
        ("staff.transfer_ownership",    "Transfer business ownership",                      "Staff"),

        // Settings
        ("settings.access",             "Access business settings",                         "Settings"),
        ("settings.billing",            "Manage billing",                                   "Settings"),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var (name, description, group) in DefaultPermissions)
        {
            var exists = unitOfWork.Query<Permission>()
                .Any(x => x.Name.ToLower() == name.ToLower());

            if (!exists)
            {
                await unitOfWork.CreateAsync(new Permission
                {
                    Name = name,
                    Description = description,
                    Group = group,
                    IsActive = true,
                    CreatedBy = SystemActor,
                    DateCreated = DateTime.UtcNow
                }, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await SeedOwnerRolePermissionsAsync(cancellationToken);
    }

    // Grant the Owner role all permissions by default
    private async Task SeedOwnerRolePermissionsAsync(CancellationToken cancellationToken)
    {
        var ownerRole = await unitOfWork.Query<Role>()
            .FirstOrDefaultAsync(x => x.Name == "Owner", cancellationToken);

        if (ownerRole is null) return;

        var allPermissions = await unitOfWork.Query<Permission>()
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var existingPermissionIds = await unitOfWork.Query<RolePermission>()
            .Where(x => x.RoleId == ownerRole.Id)
            .Select(x => x.PermissionId)
            .ToListAsync(cancellationToken);

        var missing = allPermissions.Except(existingPermissionIds).ToList();

        foreach (var permissionId in missing)
        {
            await unitOfWork.CreateAsync(new RolePermission
            {
                RoleId = ownerRole.Id,
                PermissionId = permissionId,
                IsActive = true,
                CreatedBy = SystemActor,
                DateCreated = DateTime.UtcNow
            }, cancellationToken);
        }

        if (missing.Count > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
