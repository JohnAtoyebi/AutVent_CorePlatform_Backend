using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;

namespace AutVent.CorePlatform.Api.Services;

public sealed class StaffRangeSeeder(IUnitOfWork unitOfWork)
{
    private const string SystemActor = "system";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var name in GetStaffRanges())
        {
            var exists = unitOfWork.Query<StaffRange>()
                .Any(x => x.Name == name);

            if (!exists)
            {
                var entity = new StaffRange
                {
                    Name = name,
                    IsActive = true,
                    CreatedBy = SystemActor,
                    DateCreated = DateTime.UtcNow
                };

                await unitOfWork.CreateAsync(entity, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<string> GetStaffRanges() =>
    [
        "1-10",
        "11-50",
        "51-200",
        "200+"
    ];
}
