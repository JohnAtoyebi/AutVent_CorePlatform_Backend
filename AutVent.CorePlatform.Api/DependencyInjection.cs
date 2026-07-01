using AutVent.CorePlatform.Api.Services;

namespace AutVent.CorePlatform.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IBusinessService, BusinessService>();
        services.AddScoped<IStoreService, StoreService>();
        services.AddScoped<IProductService, ProductService>();
        return services;
    }
}
