using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;

namespace AutVent.CorePlatform.Api.Services;

public sealed class StoreCategorySeeder(IUnitOfWork unitOfWork)
{
    private const string SystemActor = "system";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var categories = GetCategories();
        
        foreach (var categoryName in categories)
        {
            var exists = unitOfWork.Query<StoreCategory>()
                .Any(x => x.Name.ToLower() == categoryName.ToLower());

            if (!exists)
            {
                var entity = new StoreCategory
                {
                    Name = categoryName,
                    IsActive = true,
                    CreatedBy = SystemActor,
                    DateCreated = DateTime.UtcNow
                };

                await unitOfWork.CreateAsync(entity, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<string> GetCategories() =>
    [
        "Physical",
        "Online",
        "Mobile",
        "Physical and Online"
    ];
}
