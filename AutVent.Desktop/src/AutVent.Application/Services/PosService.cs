using System.Text.Json;
using AutVent.Application.Abstractions.Persistence;
using AutVent.Application.Abstractions.Services;
using AutVent.Application.Contracts;
using AutVent.Domain.Entities;
using AutVent.Domain.Enums;
using AutVent.Shared.Results;

namespace AutVent.Application.Services;

public sealed class PosService : IPosService
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ISalesRepository _salesRepository;
    private readonly IPendingSyncRepository _pendingSyncRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PosService(
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository,
        ISalesRepository salesRepository,
        IPendingSyncRepository pendingSyncRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _salesRepository = salesRepository;
        _pendingSyncRepository = pendingSyncRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<Product>> SearchProductsAsync(string? searchTerm, CancellationToken cancellationToken)
        => _productRepository.SearchAsync(searchTerm, cancellationToken);

    public async Task<Result<Guid>> CompleteSaleAsync(CompleteSaleCommand command, CancellationToken cancellationToken)
    {
        if (command.Lines.Count == 0)
        {
            return Result<Guid>.Failure("A sale requires at least one line item.");
        }

        var saleId = Guid.NewGuid();
        var items = command.Lines.Select(line => new SaleItem
        {
            Id = Guid.NewGuid(),
            SaleId = saleId,
            ProductId = line.ProductId,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            DiscountAmount = line.DiscountAmount,
            TaxAmount = line.TaxAmount,
            LineTotal = (line.UnitPrice * line.Quantity) - line.DiscountAmount + line.TaxAmount
        }).ToList();

        foreach (var line in command.Lines)
        {
            var stock = await _inventoryRepository.GetByProductAsync(command.StoreId, line.ProductId, cancellationToken);
            if (stock is null || stock.QuantityOnHand < line.Quantity)
            {
                return Result<Guid>.Failure("Insufficient stock for one or more selected products.");
            }

            stock.QuantityOnHand -= line.Quantity;
            stock.UpdatedAtUtc = DateTime.UtcNow;
            await _inventoryRepository.UpsertAsync([stock], cancellationToken);
        }

        var subtotal = items.Sum(item => item.UnitPrice * item.Quantity);
        var discountAmount = command.DiscountType == DiscountType.Percentage
            ? subtotal * command.DiscountValue / 100m
            : command.DiscountValue;
        var total = subtotal - discountAmount + command.TaxAmount;

        var sale = new Sale
        {
            Id = saleId,
            StoreId = command.StoreId,
            RemoteStoreId = command.RemoteStoreId,
            Subtotal = subtotal,
            DiscountAmount = discountAmount,
            TaxAmount = command.TaxAmount,
            TotalAmount = total,
            PaymentMethod = command.PaymentMethod,
            IsSynced = false,
            CreatedAtUtc = DateTime.UtcNow,
            Items = items
        };

        await _salesRepository.AddAsync(sale, cancellationToken);

        var deduplicationKey = $"SALE-{sale.Id:N}";
        if (!await _pendingSyncRepository.ExistsDeduplicationKeyAsync(deduplicationKey, cancellationToken))
        {
            await _pendingSyncRepository.AddAsync(new PendingSyncOperation
            {
                Id = Guid.NewGuid(),
                OperationType = SyncOperationType.SaleUpload,
                PayloadJson = JsonSerializer.Serialize(command),
                RetryCount = 0,
                CreatedAtUtc = DateTime.UtcNow,
                IsCompleted = false,
                DeduplicationKey = deduplicationKey
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(sale.Id);
    }
}
