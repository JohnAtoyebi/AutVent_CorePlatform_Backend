using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;

namespace AutVent.CorePlatform.Api.Services;

public sealed class BusinessIndustrySeeder(IUnitOfWork unitOfWork)
{
    private const string SystemActor = "system";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var industries = GetIndustries();
        
        foreach (var industryName in industries)
        {
            var exists = unitOfWork.Query<BusinessIndustry>()
                .Any(x => x.Name.ToLower() == industryName.ToLower());

            if (!exists)
            {
                var entity = new BusinessIndustry
                {
                    Name = industryName,
                    IsActive = true,
                    CreatedBy = SystemActor,
                    DateCreated = DateTime.UtcNow
                };

                await unitOfWork.CreateAsync(entity, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<string> GetIndustries() =>
    [
        "Retail",
        "Wholesale & Distribution",
        "Manufacturing",
        "Restaurant & Food Service",
        "Fashion & Apparel",
        "Beauty & Cosmetics",
        "Electronics & Gadgets",
        "Pharmacy & Healthcare",
        "Logistics & Delivery",
        "Professional Services",
        "Education & Training",
        "Hospitality & Tourism",
        "Real Estate & Property Management",
        "Agriculture & Agribusiness",
        "Automotive",
        "Construction & Building Materials",
        "Home & Furniture",
        "Sports & Fitness",
        "Entertainment & Media",
        "E-commerce",
        "Technology & Software",
        "Financial Services",
        "Non-Profit & NGO",
        "Religious Organization",
        "Others"
    ];
}
