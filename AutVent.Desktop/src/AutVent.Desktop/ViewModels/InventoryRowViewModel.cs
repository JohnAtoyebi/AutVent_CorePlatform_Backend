using AutVent.Application.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutVent.Desktop.ViewModels;

public sealed partial class InventoryRowViewModel : ObservableObject
{
    public InventoryRowViewModel(InventoryItemDto dto)
    {
        InventoryRecordId = dto.InventoryRecordId;
        ProductId = dto.ProductId;
        RemoteProductId = dto.RemoteProductId;
        RemoteStoreId = dto.RemoteStoreId;
        ProductName = dto.ProductName;
        Sku = dto.Sku;
        CategoryName = dto.CategoryName;
        _quantityOnHand = dto.QuantityOnHand;
        ReorderLevel = dto.ReorderLevel;
    }

    public System.Guid InventoryRecordId { get; }

    public System.Guid ProductId { get; }

    public long RemoteProductId { get; }

    public long RemoteStoreId { get; }

    public string CategoryName { get; }

    public string ProductName { get; }

    public string Sku { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLowStock))]
    [NotifyPropertyChangedFor(nameof(StockStatusLabel))]
    private decimal _quantityOnHand;

    public decimal ReorderLevel { get; }

    public bool IsLowStock => QuantityOnHand <= ReorderLevel;

    public string StockStatusLabel => IsLowStock ? "Low Stock" : "In Stock";

    public string StockStatusColor => IsLowStock ? "#EF4444" : "#10B981";

    public void ApplyDto(InventoryItemDto dto)
    {
        QuantityOnHand = dto.QuantityOnHand;
    }
}
