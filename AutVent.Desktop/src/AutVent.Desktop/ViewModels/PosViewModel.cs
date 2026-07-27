using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutVent.Application.Abstractions.Persistence;
using AutVent.Application.Abstractions.Services;
using AutVent.Application.Abstractions.System;
using AutVent.Application.Contracts;
using AutVent.Domain.Entities;
using AutVent.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutVent.Desktop.ViewModels;

public sealed record SuspendedSale(string Label, System.Collections.Generic.List<CartItemViewModel> Items);

public partial class PosViewModel : ViewModelBase
{
    private readonly IPosService _posService;
    private readonly IStoreRepository _storeRepository;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly IConnectivityService _connectivityService;

    public PosViewModel(
        IPosService posService,
        IStoreRepository storeRepository,
        IAuthSessionRepository authSessionRepository,
        IConnectivityService connectivityService)
    {
        _posService = posService;
        _storeRepository = storeRepository;
        _authSessionRepository = authSessionRepository;
        _connectivityService = connectivityService;

        PaymentMethods = Enum.GetValues<PaymentMethod>().ToList();
        _selectedPaymentMethod = PaymentMethod.Cash;
    }

    public ObservableCollection<Product> SearchResults { get; } = new();

    public ObservableCollection<CartItemViewModel> CartItems { get; } = new();

    public ObservableCollection<Store> Stores { get; } = new();

    public ObservableCollection<SuspendedSale> SuspendedSales { get; } = new();

    public System.Collections.Generic.IReadOnlyList<PaymentMethod> PaymentMethods { get; }

    [ObservableProperty]
    private Store? _selectedStore;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalAmount))]
    [NotifyPropertyChangedFor(nameof(TaxAmount))]
    private decimal _discountAmount;

    [ObservableProperty]
    private PaymentMethod _selectedPaymentMethod;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isCheckoutSuccess;

    [ObservableProperty]
    private Guid? _lastSaleId;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isOnline;

    public decimal Subtotal => CartItems.Sum(i => i.UnitPrice * i.Quantity);

    public decimal TaxAmount => CartItems.Sum(i => i.TaxAmount);

    public decimal TotalAmount => Math.Max(0, Subtotal - DiscountAmount + TaxAmount);

    public int CartCount => CartItems.Sum(i => (int)i.Quantity);

    public bool HasCartItems => CartItems.Count > 0;

    public bool HasSuspendedSales => SuspendedSales.Count > 0;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        IsOnline = _connectivityService.IsOnline();

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

    [RelayCommand]
    private async Task SearchProductsAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            SearchResults.Clear();
            return;
        }

        IsLoading = true;

        try
        {
            var results = await _posService.SearchProductsAsync(SearchTerm.Trim(), cancellationToken);
            SearchResults.Clear();
            foreach (var product in results.Where(p => p.IsActive))
            {
                SearchResults.Add(product);
            }
        }
        catch (Exception ex)
        {
            SetError($"Product search failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void AddToCart(Product product)
    {
        ClearStatus();
        var existing = CartItems.FirstOrDefault(i => i.ProductId == product.Id);

        if (existing is not null)
        {
            existing.Quantity++;
        }
        else
        {
            CartItems.Add(new CartItemViewModel(product, RemoveFromCart));
        }

        RefreshCartTotals();
        SearchTerm = string.Empty;
        SearchResults.Clear();
    }

    private void RemoveFromCart(CartItemViewModel item)
    {
        CartItems.Remove(item);
        RefreshCartTotals();
    }

    [RelayCommand]
    private void ClearCart()
    {
        CartItems.Clear();
        DiscountAmount = 0;
        IsCheckoutSuccess = false;
        RefreshCartTotals();
        ClearStatus();
    }

    [RelayCommand(CanExecute = nameof(CanCheckout))]
    private async Task CheckoutAsync(CancellationToken cancellationToken)
    {
        if (SelectedStore is null || CartItems.Count == 0) return;

        IsLoading = true;
        ClearStatus();
        IsCheckoutSuccess = false;

        try
        {
            var lines = CartItems.Select(item => new SaleLineCommand(
                ProductId: item.ProductId,
                RemoteProductId: item.Product.RemoteId,
                Quantity: item.Quantity,
                UnitPrice: item.UnitPrice,
                DiscountAmount: item.DiscountAmount,
                TaxAmount: item.TaxAmount)).ToList();

            var command = new CompleteSaleCommand(
                StoreId: SelectedStore.Id,
                RemoteStoreId: SelectedStore.RemoteId,
                RemoteCustomerId: null,
                Lines: lines,
                AmountPaid: TotalAmount,
                PaymentMethod: SelectedPaymentMethod,
                DiscountType: DiscountType.Amount,
                DiscountValue: DiscountAmount,
                TaxAmount: TaxAmount,
                Notes: null,
                BalanceDueDate: null,
                ExpectedBalanceRemaining: null);

            var result = await _posService.CompleteSaleAsync(command, cancellationToken);

            if (result.IsFailure)
            {
                SetError(result.Error ?? "Checkout failed.");
                return;
            }

            LastSaleId = result.Value;
            IsCheckoutSuccess = true;
            SetStatus($"Sale #{result.Value.ToString()[..8].ToUpperInvariant()} completed. Total: {TotalAmount:C2}");
            CartItems.Clear();
            DiscountAmount = 0;
            RefreshCartTotals();
        }
        catch (Exception ex)
        {
            SetError($"Checkout failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanCheckout() => CartItems.Count > 0 && SelectedStore is not null && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanSuspend))]
    private void SuspendSale()
    {
        if (CartItems.Count == 0) return;

        var label = $"Sale {SuspendedSales.Count + 1} — {DateTime.Now:HH:mm}";
        var snapshot = CartItems.ToList();
        SuspendedSales.Add(new SuspendedSale(label, snapshot));

        CartItems.Clear();
        DiscountAmount = 0;
        RefreshCartTotals();
        ClearStatus();

        OnPropertyChanged(nameof(HasSuspendedSales));
    }

    private bool CanSuspend() => CartItems.Count > 0 && !IsLoading;

    [RelayCommand]
    private void ResumeSale(SuspendedSale suspended)
    {
        if (CartItems.Count > 0)
        {
            SetError("Please clear or complete the current sale before resuming another.");
            return;
        }

        foreach (var item in suspended.Items)
        {
            CartItems.Add(item);
        }

        SuspendedSales.Remove(suspended);
        OnPropertyChanged(nameof(HasSuspendedSales));
        RefreshCartTotals();
        ClearStatus();
    }

    partial void OnDiscountAmountChanged(decimal value) => RefreshCartTotals();

    private void RefreshCartTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(TaxAmount));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(CartCount));
        OnPropertyChanged(nameof(HasCartItems));
        CheckoutCommand.NotifyCanExecuteChanged();
        SuspendSaleCommand.NotifyCanExecuteChanged();
    }

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

