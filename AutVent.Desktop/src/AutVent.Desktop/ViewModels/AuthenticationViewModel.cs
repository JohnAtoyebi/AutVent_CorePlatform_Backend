using System.Threading;
using System.Threading.Tasks;
using AutVent.Application.Abstractions.Services;
using AutVent.Application.Abstractions.System;
using AutVent.Application.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutVent.Desktop.ViewModels;

public partial class AuthenticationViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authService;
    private readonly IConnectivityService _connectivityService;
    private readonly INavigationService _navigationService;

    public AuthenticationViewModel(
        IAuthenticationService authService,
        IConnectivityService connectivityService,
        INavigationService navigationService)
    {
        _authService = authService;
        _connectivityService = connectivityService;
        _navigationService = navigationService;

        IsOnline = _connectivityService.IsOnline();
    }

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberMe = true;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isOnline;

    [ObservableProperty]
    private bool _isOfflineModeAvailable;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private AuthSessionDto? _currentSession;

    [ObservableProperty]
    private bool _isAuthenticated;

    public string ConnectivityLabel => IsOnline ? "Online" : "Offline";

    partial void OnIsOnlineChanged(bool value) => OnPropertyChanged(nameof(ConnectivityLabel));

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IsOnline = _connectivityService.IsOnline();

        var session = await _authService.TryOfflineLoginAsync(string.Empty, cancellationToken);
        IsOfflineModeAvailable = session.IsSuccess;
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        ClearError();
        IsLoading = true;

        try
        {
            IsOnline = _connectivityService.IsOnline();

            bool succeeded;
            string? failureMessage;

            if (IsOnline)
            {
                if (string.IsNullOrWhiteSpace(Password))
                {
                    SetError("Password is required.");
                    return;
                }

                var onlineResult = await _authService.LoginAsync(
                    new LoginRequest(Email.Trim(), Password, RememberMe),
                    cancellationToken);

                succeeded = onlineResult.IsSuccess;
                failureMessage = onlineResult.Error;

                if (onlineResult.IsSuccess)
                {
                    CurrentSession = onlineResult.Value;
                }
            }
            else
            {
                var offlineResult = await _authService.TryOfflineLoginAsync(Email.Trim(), cancellationToken);
                succeeded = offlineResult.IsSuccess;
                failureMessage = offlineResult.Error;

                if (offlineResult.IsSuccess)
                {
                    CurrentSession = offlineResult.Value;
                }
            }

            if (!succeeded)
            {
                SetError(failureMessage ?? "Authentication failed.");
                return;
            }

            IsAuthenticated = true;
            Password = string.Empty;
            _navigationService.Navigate("pos");
        }
        catch (System.Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanLogin() => !IsLoading && !string.IsNullOrWhiteSpace(Email);

    partial void OnEmailChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
    partial void OnIsLoadingChanged(bool value) => LoginCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private async Task LogoutAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        ClearError();

        try
        {
            await _authService.LogoutAsync(cancellationToken);
            IsAuthenticated = false;
            CurrentSession = null;
            IsOfflineModeAvailable = false;
            Email = string.Empty;
        }
        catch (System.Exception ex)
        {
            SetError($"Logout error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }
}

