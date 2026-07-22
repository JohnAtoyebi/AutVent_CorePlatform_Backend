using System;
using System.Collections.Generic;
using AutVent.Application.Abstractions.System;
using AutVent.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AutVent.Desktop.Services;

public sealed class NavigationService : INavigationService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IReadOnlyDictionary<string, Type> _routes;
    private IServiceScope? _currentScope;

    public NavigationService(IServiceScopeFactory scopeFactory, IReadOnlyDictionary<string, Type> routes)
    {
        _scopeFactory = scopeFactory;
        _routes = routes;
    }

    public ViewModelBase? CurrentViewModel { get; private set; }

    public event EventHandler<ViewModelBase>? CurrentViewChanged;

    public void Navigate(string routeKey)
    {
        if (!_routes.TryGetValue(routeKey, out var vmType))
        {
            return;
        }

        _currentScope?.Dispose();
        _currentScope = _scopeFactory.CreateScope();

        CurrentViewModel = (ViewModelBase)_currentScope.ServiceProvider.GetRequiredService(vmType);
        CurrentViewChanged?.Invoke(this, CurrentViewModel);
    }

    public void Dispose()
    {
        _currentScope?.Dispose();
    }
}

