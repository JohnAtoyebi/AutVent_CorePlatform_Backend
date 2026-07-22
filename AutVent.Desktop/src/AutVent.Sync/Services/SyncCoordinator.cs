using System.Text.Json;
using AutVent.Application.Abstractions.Api;
using AutVent.Application.Abstractions.Persistence;
using AutVent.Application.Abstractions.Services;
using AutVent.Application.Abstractions.System;
using AutVent.Application.Contracts;
using AutVent.Domain.Enums;

namespace AutVent.Sync.Services;

public sealed class SyncCoordinator : ISyncCoordinator
{
    private readonly IConnectivityService _connectivityService;
    private readonly IPendingSyncRepository _pendingSyncRepository;
    private readonly ISalesApiClient _salesApiClient;
    private readonly IInventoryApiClient _inventoryApiClient;
    private readonly ICatalogApiClient _catalogApiClient;
    private readonly IProductRepository _productRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SyncCoordinator(
        IConnectivityService connectivityService,
        IPendingSyncRepository pendingSyncRepository,
        ISalesApiClient salesApiClient,
        IInventoryApiClient inventoryApiClient,
        ICatalogApiClient catalogApiClient,
        IProductRepository productRepository,
        IStoreRepository storeRepository,
        IUnitOfWork unitOfWork)
    {
        _connectivityService = connectivityService;
        _pendingSyncRepository = pendingSyncRepository;
        _salesApiClient = salesApiClient;
        _inventoryApiClient = inventoryApiClient;
        _catalogApiClient = catalogApiClient;
        _productRepository = productRepository;
        _storeRepository = storeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        if (!_connectivityService.IsOnline())
        {
            return;
        }

        // ── Upload pending operations ────────────────────────────────────────
        var pending = await _pendingSyncRepository.GetPendingAsync(cancellationToken);

        foreach (var operation in pending)
        {
            try
            {
                switch (operation.OperationType)
                {
                    case SyncOperationType.SaleUpload:
                    {
                        var command = JsonSerializer.Deserialize<CompleteSaleCommand>(operation.PayloadJson);
                        if (command is not null)
                        {
                            await _salesApiClient.CheckoutAsync(command, cancellationToken);
                        }
                        break;
                    }
                    case SyncOperationType.InventoryAdjustmentUpload:
                    {
                        var command = JsonSerializer.Deserialize<AdjustInventoryCommand>(operation.PayloadJson);
                        if (command is not null)
                        {
                            await _inventoryApiClient.UploadAdjustmentAsync(command, operation.DeduplicationKey, cancellationToken);
                        }
                        break;
                    }
                }

                await _pendingSyncRepository.MarkCompletedAsync(operation.Id, cancellationToken);
            }
            catch
            {
                await _pendingSyncRepository.IncrementRetryAsync(operation.Id, cancellationToken);
            }
        }

        // ── Pull latest catalog data ─────────────────────────────────────────
        var stores = await _catalogApiClient.GetStoresAsync(cancellationToken);
        await _storeRepository.UpsertAsync(stores, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var store in stores)
        {
            var products = await _catalogApiClient.GetProductsAsync(store.RemoteId, cancellationToken);
            await _productRepository.UpsertAsync(products, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
