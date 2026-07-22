using AutVent.Application.Abstractions.Services;
using AutVent.Sync.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AutVent.Sync;

public static class DependencyInjection
{
    public static IServiceCollection AddSyncEngine(this IServiceCollection services)
    {
        services.AddScoped<ISyncCoordinator, SyncCoordinator>();
        services.AddHostedService<SyncBackgroundService>();

        return services;
    }
}
