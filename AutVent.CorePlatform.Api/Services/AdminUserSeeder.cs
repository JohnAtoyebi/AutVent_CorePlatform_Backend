using AutVent.CorePlatform.Api.Common.Security;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class AdminUserSeeder(IUnitOfWork unitOfWork)
{
    private const string SystemActor = "system";
    private const string AdminEmail = "hello@autvent.com";
    private const string AdminPassword = "Admin@1234";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var adminRole = await unitOfWork.Query<Role>()
            .FirstOrDefaultAsync(x => x.Name == "BackOfficeAdmin", cancellationToken);

        if (adminRole is null)
        {
            throw new InvalidOperationException("BackOfficeAdmin role is missing. Run role seeding first.");
        }

        var now = DateTime.UtcNow;
        var adminUser = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(x => x.EmailAddress.ToLower() == AdminEmail, cancellationToken);

        if (adminUser is null)
        {
            adminUser = new User
            {
                FullName = "System Admin",
                EmailAddress = AdminEmail,
                PhoneNumber = "08000000000",
                Password = PasswordHasher.Hash(AdminPassword),
                IsEmailVerified = true,
                IsActive = true,
                RoleId = adminRole.Id,
                CreatedBy = SystemActor,
                DateCreated = now
            };

            await unitOfWork.CreateAsync(adminUser, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var hasChanges = false;

        if (!string.Equals(adminUser.FullName, "System Admin", StringComparison.Ordinal))
        {
            adminUser.FullName = "System Admin";
            hasChanges = true;
        }

        if (!string.Equals(adminUser.PhoneNumber, "08000000000", StringComparison.Ordinal))
        {
            adminUser.PhoneNumber = "08000000000";
            hasChanges = true;
        }

        var passwordHash = PasswordHasher.Hash(AdminPassword);
        if (!string.Equals(adminUser.Password, passwordHash, StringComparison.Ordinal))
        {
            adminUser.Password = passwordHash;
            hasChanges = true;
        }

        if (adminUser.RoleId != adminRole.Id)
        {
            adminUser.RoleId = adminRole.Id;
            hasChanges = true;
        }

        if (!adminUser.IsEmailVerified)
        {
            adminUser.IsEmailVerified = true;
            hasChanges = true;
        }

        if (!adminUser.IsActive)
        {
            adminUser.IsActive = true;
            hasChanges = true;
        }

        if (hasChanges)
        {
            adminUser.UpdatedBy = SystemActor;
            adminUser.DateUpdated = now;
            unitOfWork.Update(adminUser);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
