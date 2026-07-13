using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;

namespace AutVent.CorePlatform.Api.Services;

public sealed class SubscriptionPlanSeeder(IUnitOfWork unitOfWork)
{
    private const string SystemActor = "system";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var plans = new[]
        {
            (
                Plan: SubscriptionPlan.Starter,
                Name: "Starter",
                Description: "Perfect for small businesses just getting started.",
                MonthlyPrice: 0m,
                AnnualPrice: 0m,
                MaxStores: 1,
                MaxStaff: 3,
                MaxProducts: 100
            ),
            (
                Plan: SubscriptionPlan.Growth,
                Name: "Growth",
                Description: "For growing businesses that need more power and flexibility.",
                MonthlyPrice: 29.99m,
                AnnualPrice: 299.99m,
                MaxStores: 5,
                MaxStaff: 15,
                MaxProducts: 1000
            ),
            (
                Plan: SubscriptionPlan.Enterprise,
                Name: "Enterprise",
                Description: "Unlimited access for large-scale operations.",
                MonthlyPrice: 99.99m,
                AnnualPrice: 999.99m,
                MaxStores: (int?)null,
                MaxStaff: (int?)null,
                MaxProducts: (int?)null
            )
        };

        foreach (var p in plans)
        {
            var exists = unitOfWork.Query<SubscriptionPlanDefinition>()
                .Any(x => x.Plan == p.Plan);

            if (!exists)
            {
                await unitOfWork.CreateAsync(new SubscriptionPlanDefinition
                {
                    Plan = p.Plan,
                    Name = p.Name,
                    Description = p.Description,
                    MonthlyPrice = p.MonthlyPrice,
                    AnnualPrice = p.AnnualPrice,
                    MaxStores = p.MaxStores,
                    MaxStaff = p.MaxStaff,
                    MaxProducts = p.MaxProducts,
                    IsActive = true,
                    CreatedBy = SystemActor,
                    DateCreated = DateTime.UtcNow
                }, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
