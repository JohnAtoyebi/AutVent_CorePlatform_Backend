using System;
using Avalonia;
using AutVent.Application;
using AutVent.Infrastructure;
using AutVent.Infrastructure.Data;
using AutVent.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AutVent.Desktop;

sealed class Program
{
    public static IHost AppHost { get; private set; } = CreateHostBuilder().Build();

    [STAThread]
    public static void Main(string[] args)
    {
        AppHost.Start();

        using var scope = AppHost.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AutVentDbContext>();
        dbContext.Database.Migrate();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        AppHost.Dispose();
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();

    private static IHostBuilder CreateHostBuilder()
        => Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(configuration =>
            {
                configuration.SetBasePath(AppContext.BaseDirectory);
                configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddApplication();
                services.AddInfrastructure(context.Configuration);
                services.AddSyncEngine();
                services.AddDesktopPresentation();
            });
}
