using System.Collections.ObjectModel;
using AutVent.Desktop.Models;
using AutVent.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutVent.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly NavigationService _navigationService;

    public MainViewModel(NavigationService navigationService)
    {
        _navigationService = navigationService;
        Modules = new ObservableCollection<NavigationItem>
        {
            new("auth", "Authentication"),
            new("inventory", "Inventory"),
            new("pos", "POS")
        };

        _navigationService.CurrentViewChanged += OnCurrentViewChanged;
        _navigationService.Navigate("auth");
    }

    public ObservableCollection<NavigationItem> Modules { get; }

    [ObservableProperty]
    private string _currentModuleTitle = "Authentication";

    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    [ObservableProperty]
    private bool _isSidebarCollapsed;

    [ObservableProperty]
    private double _sidebarWidth = 260;

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
        SidebarWidth = IsSidebarCollapsed ? 88 : 260;
    }

    [RelayCommand]
    private void Navigate(string routeKey)
    {
        _navigationService.Navigate(routeKey);
    }

    private void OnCurrentViewChanged(object? sender, ViewModelBase viewModel)
    {
        CurrentViewModel = viewModel;
        CurrentModuleTitle = viewModel switch
        {
            AuthenticationViewModel => "Authentication",
            InventoryViewModel => "Inventory",
            PosViewModel => "POS",
            _ => "AutVent"
        };
    }
}
