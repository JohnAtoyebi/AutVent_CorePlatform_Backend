using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;

namespace AutVent.CorePlatform.Api.Services;

public sealed class RoleSeeder(IUnitOfWork unitOfWork)
{
    private const string SystemActor = "system";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var defaultRoles = new[]
        {
            ("Owner",  "Full access to all business operations and settings"),
            ("Admin",  "Manages store operations, staff, and configurations"),
            ("Staff",  "Handles day-to-day store activities such as sales and inventory"),
            ("Viewer", "Read-only access to store data and reports")
        };

        foreach (var (name, description) in defaultRoles)
        {
            var exists = unitOfWork.Query<Role>()
                .Any(x => x.Name.ToLower() == name.ToLower());

            if (!exists)
            {
                await unitOfWork.CreateAsync(new Role
                {
                    Name = name,
                    Description = description,
                    IsDefault = true,
                    IsActive = true,
                    CreatedBy = SystemActor,
                    DateCreated = DateTime.UtcNow
                }, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
