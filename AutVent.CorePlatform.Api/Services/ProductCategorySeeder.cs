using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;

namespace AutVent.CorePlatform.Api.Services;

public sealed class ProductCategorySeeder(IUnitOfWork unitOfWork)
{
    private const string SystemActor = "system";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var categories = GetCategories();

        foreach (var categoryName in categories)
        {
            var existing = unitOfWork.Query<ProductCategory>()
                .FirstOrDefault(x => x.Name.ToLower() == categoryName.ToLower());

            if (existing is null)
            {
                var entity = new ProductCategory
                {
                    Name = categoryName,
                    IsDefault = true,
                    IsActive = true,
                    CreatedBy = SystemActor,
                    DateCreated = DateTime.UtcNow
                };

                await unitOfWork.CreateAsync(entity, cancellationToken);
            }
            else if (!existing.IsDefault)
            {
                // Backfill: mark any pre-existing seeded category as default
                existing.IsDefault = true;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<string> GetCategories() =>
    [
        "Electronics & Gadgets",
        "Hardware & Tools",
        "Fashion & Apparel",
        "Beauty & Cosmetics",
        "Food & Beverages",
        "Groceries",
        "Furnitures",
        "Health",
        "Others"
    ];
}

