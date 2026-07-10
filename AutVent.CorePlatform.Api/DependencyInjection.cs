using AutVent.CorePlatform.Api.Common;
using AutVent.CorePlatform.Api.Common.Email;
using AutVent.CorePlatform.Api.Infrastructure.Email;
using AutVent.CorePlatform.Api.Services;

namespace AutVent.CorePlatform.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection("Email"));
        services.Configure<AppOptions>(configuration.GetSection("App"));
        services.AddHttpClient(nameof(ResendEmailProvider));
        services.AddTransient<ResendEmailProvider>();
        services.AddScoped<IEmailProvider, EmailProviderFactory>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<IReferralService, ReferralService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBusinessService, BusinessService>();
        services.AddScoped<IStoreService, StoreService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IStaffRangeService, StaffRangeService>();
        services.AddScoped<IBusinessIndustryService, BusinessIndustryService>();
        services.AddScoped<IProductCategoryService, ProductCategoryService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IPosService, PosService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<BusinessIndustrySeeder>();
        services.AddScoped<StaffRangeSeeder>();
        services.AddScoped<StoreCategorySeeder>();
        services.AddScoped<ProductCategorySeeder>();
        return services;
    }
}
