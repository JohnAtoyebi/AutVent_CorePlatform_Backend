using System;
using System.Collections.Generic;
using AutVent.Application.Abstractions.System;
using AutVent.Desktop.Services;
using AutVent.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AutVent.Desktop;

public static class DependencyInjection
{
    public static IServiceCollection AddDesktopPresentation(this IServiceCollection services)
    {
        services.AddTransient<AuthenticationViewModel>();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<PosViewModel>();

        services.AddSingleton<IReadOnlyDictionary<string, Type>>(_ =>
            new Dictionary<string, Type>
            {
                ["auth"] = typeof(AuthenticationViewModel),
                ["inventory"] = typeof(InventoryViewModel),
                ["pos"] = typeof(PosViewModel)
            });

        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());
        services.AddSingleton<MainViewModel>();

        return services;
    }
}

