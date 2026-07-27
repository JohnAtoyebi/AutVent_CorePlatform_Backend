using AutVent.Application.Abstractions.Services;
using AutVent.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AutVent.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IPosService, PosService>();

        return services;
    }
}
