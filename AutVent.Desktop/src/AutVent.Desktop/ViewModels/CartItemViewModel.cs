using System;
using AutVent.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutVent.Desktop.ViewModels;

public sealed partial class CartItemViewModel : ObservableObject
{
    private readonly Action<CartItemViewModel> _onRemove;

    public CartItemViewModel(Product product, Action<CartItemViewModel> onRemove)
    {
        Product = product;
        _onRemove = onRemove;
        _unitPrice = product.UnitPrice;
        _quantity = 1;
    }

    public Product Product { get; }

    public Guid ProductId => Product.Id;

    public string ProductName => Product.Name;

    public string Sku => Product.Sku;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    private decimal _quantity;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    private decimal _unitPrice;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    private decimal _discountAmount;

    public decimal TaxAmount => Math.Round((_unitPrice * _quantity - _discountAmount) * Product.TaxRate, 2);

    public decimal LineTotal => Math.Round(_unitPrice * _quantity - _discountAmount + TaxAmount, 2);

    partial void OnQuantityChanged(decimal value)
    {
        if (value < 1) Quantity = 1;
        OnPropertyChanged(nameof(TaxAmount));
        DecreaseQuantityCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void IncreaseQuantity() => Quantity++;

    [RelayCommand(CanExecute = nameof(CanDecreaseQuantity))]
    private void DecreaseQuantity()
    {
        if (Quantity > 1) Quantity--;
    }

    private bool CanDecreaseQuantity() => Quantity > 1;

    [RelayCommand]
    private void Remove() => _onRemove(this);
}
