using System.Text.Json;
using AutVent.Application.Abstractions.Persistence;
using AutVent.Application.Abstractions.Services;
using AutVent.Application.Contracts;
using AutVent.Domain.Entities;
using AutVent.Domain.Enums;
using AutVent.Shared.Results;

namespace AutVent.Application.Services;

public sealed class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPendingSyncRepository _pendingSyncRepository;
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(
        IInventoryRepository inventoryRepository,
        IProductRepository productRepository,
        IPendingSyncRepository pendingSyncRepository,
        IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _productRepository = productRepository;
        _pendingSyncRepository = pendingSyncRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<InventoryRecord>> GetInventoryAsync(Guid storeId, string? searchTerm, CancellationToken cancellationToken)
        => _inventoryRepository.GetByStoreAsync(storeId, searchTerm, cancellationToken);

    public async Task<IReadOnlyList<InventoryItemDto>> GetInventoryItemsAsync(Guid storeId, string? searchTerm, CancellationToken cancellationToken)
    {
        var records = await _inventoryRepository.GetByStoreAsync(storeId, searchTerm, cancellationToken);
        var productIds = records.Select(r => r.ProductId).ToHashSet();
        var allProducts = await _productRepository.SearchAsync(null, cancellationToken);
        var productMap = allProducts.Where(p => productIds.Contains(p.Id)).ToDictionary(p => p.Id);

        return records.Select(r =>
        {
            productMap.TryGetValue(r.ProductId, out var product);
            return new InventoryItemDto(
                InventoryRecordId: r.Id,
                ProductId: r.ProductId,
                RemoteProductId: r.RemoteProductId,
                RemoteStoreId: r.RemoteStoreId,
                ProductName: product?.Name ?? "Unknown Product",
                Sku: product?.Sku ?? string.Empty,
                CategoryName: product?.CategoryName ?? string.Empty,
                QuantityOnHand: r.QuantityOnHand,
                ReorderLevel: r.ReorderLevel,
                IsLowStock: r.IsLowStock,
                IsActive: product?.IsActive ?? false);
        }).ToList();
    }

    public async Task<Result> AdjustStockAsync(AdjustInventoryCommand command, CancellationToken cancellationToken)
    {
        var current = await _inventoryRepository.GetByProductAsync(command.StoreId, command.ProductId, cancellationToken);
        if (current is null)
        {
            return Result.Failure("Inventory item was not found for the selected store.");
        }

        var delta = command.AdjustmentType == StockAdjustmentType.StockIn
            ? command.Quantity
            : -command.Quantity;

        current.QuantityOnHand += delta;
        current.UpdatedAtUtc = DateTime.UtcNow;

        await _inventoryRepository.UpsertAsync([current], cancellationToken);

        var deduplicationKey = $"INV-{command.StoreId:N}-{command.ProductId:N}-{DateTime.UtcNow.Ticks}";
        if (!await _pendingSyncRepository.ExistsDeduplicationKeyAsync(deduplicationKey, cancellationToken))
        {
            await _pendingSyncRepository.AddAsync(new PendingSyncOperation
            {
                Id = Guid.NewGuid(),
                OperationType = SyncOperationType.InventoryAdjustmentUpload,
                PayloadJson = JsonSerializer.Serialize(command),
                RetryCount = 0,
                CreatedAtUtc = DateTime.UtcNow,
                IsCompleted = false,
                DeduplicationKey = deduplicationKey
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
