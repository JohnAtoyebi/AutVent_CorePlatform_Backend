using AutVent.Application.Abstractions.Api;
using AutVent.Application.Abstractions.Persistence;
using AutVent.Application.Abstractions.Security;
using AutVent.Application.Abstractions.System;
using AutVent.Infrastructure.Api;
using AutVent.Infrastructure.Data;
using AutVent.Infrastructure.Http;
using AutVent.Infrastructure.Persistence;
using AutVent.Infrastructure.Security;
using AutVent.Infrastructure.System;
using AutVent.Shared.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutVent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var apiOptions = configuration.GetSection(ApiOptions.SectionName).Get<ApiOptions>() ?? new ApiOptions();
        var dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutVent", "desktop.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dataPath)!);

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutVent", "keys")))
            .SetApplicationName("AutVent.Desktop");

        services.AddDbContext<AutVentDbContext>(options => options.UseSqlite($"Data Source={dataPath}"));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ISalesRepository, SalesRepository>();
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<ISettingRepository, SettingRepository>();
        services.AddScoped<IPendingSyncRepository, PendingSyncRepository>();
        services.AddScoped<IAuthSessionRepository, AuthSessionRepository>();
        services.AddScoped<IConnectivityService, NetworkConnectivityService>();

        services.AddSingleton<ITokenStore, InMemoryTokenStore>();
        services.AddTransient<AccessTokenHandler>();

        services.AddHttpClient<IAuthApiClient, AuthApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(apiOptions.TimeoutSeconds);
        });

        services.AddHttpClient<ICatalogApiClient, CatalogApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(apiOptions.TimeoutSeconds);
        }).AddHttpMessageHandler<AccessTokenHandler>();

        services.AddHttpClient<IInventoryApiClient, InventoryApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(apiOptions.TimeoutSeconds);
        }).AddHttpMessageHandler<AccessTokenHandler>();

        services.AddHttpClient<ISalesApiClient, SalesApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(apiOptions.TimeoutSeconds);
        }).AddHttpMessageHandler<AccessTokenHandler>();

        return services;
    }
}
