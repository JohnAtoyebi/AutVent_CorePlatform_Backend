using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutVent.Application.Abstractions.Persistence;
using AutVent.Application.Abstractions.Services;
using AutVent.Application.Contracts;
using AutVent.Domain.Entities;
using AutVent.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutVent.Desktop.ViewModels;

public partial class InventoryViewModel : ViewModelBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IStoreRepository _storeRepository;
    private readonly IAuthSessionRepository _authSessionRepository;

    public InventoryViewModel(
        IInventoryService inventoryService,
        IStoreRepository storeRepository,
        IAuthSessionRepository authSessionRepository)
    {
        _inventoryService = inventoryService;
        _storeRepository = storeRepository;
        _authSessionRepository = authSessionRepository;
    }

    public ObservableCollection<InventoryRowViewModel> Items { get; } = new();

    public ObservableCollection<Store> Stores { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadInventoryCommand))]
    private Store? _selectedStore;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private InventoryRowViewModel? _selectedItem;

    [ObservableProperty]
    private decimal _adjustDelta;

    [ObservableProperty]
    private string _adjustReason = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isAdjustPanelOpen;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private int _totalItems;

    [ObservableProperty]
    private int _lowStockCount;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        ClearStatus();

        try
        {
            var stores = await _storeRepository.ListAsync(cancellationToken);
            Stores.Clear();
            foreach (var store in stores.Where(s => s.IsActive))
            {
                Stores.Add(store);
            }

            if (Stores.Count > 0)
            {
                SelectedStore = Stores[0];
                await LoadInventoryAsync(cancellationToken);
            }
            else
            {
                SetStatus("No active stores found. Please sync data from the server.");
            }
        }
        catch (Exception ex)
        {
            SetError($"Failed to load stores: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanLoadInventory))]
    private async Task LoadInventoryAsync(CancellationToken cancellationToken)
    {
        if (SelectedStore is null) return;

        IsLoading = true;
        ClearStatus();

        try
        {
            var items = await _inventoryService.GetInventoryItemsAsync(
                SelectedStore.Id,
                string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm.Trim(),
                cancellationToken);

            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(new InventoryRowViewModel(item));
            }

            TotalItems = Items.Count;
            LowStockCount = Items.Count(i => i.IsLowStock);

            if (Items.Count == 0)
            {
                SetStatus("No inventory records found. Try adjusting your search or syncing from the server.");
            }
        }
        catch (Exception ex)
        {
            SetError($"Failed to load inventory: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanLoadInventory() => SelectedStore is not null && !IsLoading;

    partial void OnSelectedStoreChanged(Store? value)
    {
        if (value is not null && !IsLoading)
        {
            _ = LoadInventoryAsync(CancellationToken.None);
        }
    }

    [RelayCommand(CanExecute = nameof(CanAdjust))]
    private async Task StockInAsync(CancellationToken cancellationToken)
        => await AdjustAsync(Math.Abs(AdjustDelta), cancellationToken);

    [RelayCommand(CanExecute = nameof(CanAdjust))]
    private async Task StockOutAsync(CancellationToken cancellationToken)
        => await AdjustAsync(-Math.Abs(AdjustDelta), cancellationToken);

    [RelayCommand(CanExecute = nameof(CanAdjust))]
    private async Task AdjustAsync(decimal delta, CancellationToken cancellationToken)
    {
        if (SelectedItem is null || SelectedStore is null) return;

        ClearStatus();
        IsLoading = true;

        try
        {
            var session = await _authSessionRepository.GetCurrentSessionAsync(cancellationToken);
            var performedBy = session?.Email ?? "Unknown";

            var (adjType, qty) = delta >= 0
                ? (StockAdjustmentType.StockIn, (long)Math.Abs(delta))
                : (StockAdjustmentType.StockOut, (long)Math.Abs(delta));

            var command = new AdjustInventoryCommand(
                ProductId: SelectedItem.ProductId,
                RemoteProductId: SelectedItem.RemoteProductId,
                StoreId: SelectedStore.Id,
                RemoteStoreId: SelectedItem.RemoteStoreId,
                Quantity: qty,
                AdjustmentType: adjType,
                Reason: StockAdjustmentReason.Correction,
                Notes: string.IsNullOrWhiteSpace(AdjustReason) ? null : AdjustReason,
                PerformedBy: performedBy);

            var result = await _inventoryService.AdjustStockAsync(command, cancellationToken);

            if (result.IsFailure)
            {
                SetError(result.Error ?? "Adjustment failed.");
                return;
            }

            SelectedItem.QuantityOnHand += delta;
            LowStockCount = Items.Count(i => i.IsLowStock);
            AdjustDelta = 0;
            AdjustReason = string.Empty;
            IsAdjustPanelOpen = false;
            SetStatus($"Stock adjusted successfully. New quantity: {SelectedItem.QuantityOnHand:N0}");
        }
        catch (Exception ex)
        {
            SetError($"Adjustment failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanAdjust() => SelectedItem is not null && SelectedStore is not null && AdjustDelta != 0 && !IsLoading;

    partial void OnAdjustDeltaChanged(decimal value)
    {
        StockInCommand.NotifyCanExecuteChanged();
        StockOutCommand.NotifyCanExecuteChanged();
        AdjustCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedItemChanged(InventoryRowViewModel? value)
    {
        StockInCommand.NotifyCanExecuteChanged();
        StockOutCommand.NotifyCanExecuteChanged();
        AdjustCommand.NotifyCanExecuteChanged();
        IsAdjustPanelOpen = value is not null;
        AdjustDelta = 0;
        AdjustReason = string.Empty;
    }

    [RelayCommand]
    private void SelectItem(InventoryRowViewModel item)
    {
        SelectedItem = item;
        IsAdjustPanelOpen = true;
    }

    [RelayCommand]
    private void CloseAdjustPanel()
    {
        IsAdjustPanelOpen = false;
        SelectedItem = null;
        AdjustDelta = 0;
        AdjustReason = string.Empty;
    }

    [RelayCommand]
    private async Task SearchAsync(CancellationToken cancellationToken)
        => await LoadInventoryAsync(cancellationToken);

    private void SetStatus(string message)
    {
        StatusMessage = message;
        HasError = false;
    }

    private void SetError(string message)
    {
        StatusMessage = message;
        HasError = true;
    }

    private void ClearStatus()
    {
        StatusMessage = string.Empty;
        HasError = false;
    }
}

