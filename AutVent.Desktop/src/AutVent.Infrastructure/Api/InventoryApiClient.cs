using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AutVent.Application.Abstractions.Api;
using AutVent.Application.Contracts;
using AutVent.Domain.Entities;
using AutVent.Domain.Enums;

namespace AutVent.Infrastructure.Api;

// ── Internal API shapes ──────────────────────────────────────────────────────

file sealed record InventoryItemApiResponse(
    [property: JsonPropertyName("productId")] long ProductId,
    [property: JsonPropertyName("storeId")] long StoreId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("quantity")] decimal Quantity,
    [property: JsonPropertyName("reorderThreshold")] decimal ReorderThreshold,
    [property: JsonPropertyName("isLowStock")] bool IsLowStock,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("productCategory")] string? ProductCategory,
    [property: JsonPropertyName("costPrice")] decimal CostPrice);

file sealed record UpdateInventoryStockRequestBody(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("quantity")] long Quantity,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("locationStoreId")] long LocationStoreId,
    [property: JsonPropertyName("notes")] string? Notes);

// ── Client ───────────────────────────────────────────────────────────────────

public sealed class InventoryApiClient : ApiClientBase, IInventoryApiClient
{
    private readonly HttpClient _httpClient;

    public InventoryApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<InventoryRecord>> GetInventoryAsync(
        long remoteStoreId, Guid localStoreId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"api/Inventory/store/{remoteStoreId}/items", cancellationToken);
        var items = await ReadApiResponseAsync<List<InventoryItemApiResponse>>(response, cancellationToken);

        return items.Select(item => new InventoryRecord
        {
            Id = Guid.NewGuid(),
            StoreId = localStoreId,
            RemoteStoreId = item.StoreId,
            RemoteProductId = item.ProductId,
            // ProductId is resolved later during upsert by RemoteProductId
            ProductId = Guid.Empty,
            QuantityOnHand = item.Quantity,
            ReorderLevel = item.ReorderThreshold,
            IsLowStock = item.IsLowStock,
            UpdatedAtUtc = DateTime.UtcNow
        }).ToList();
    }

    public async Task UploadAdjustmentAsync(
        AdjustInventoryCommand command, string deduplicationKey, CancellationToken cancellationToken)
    {
        var body = new UpdateInventoryStockRequestBody(
            Type: command.AdjustmentType.ToString(),
            Quantity: command.Quantity,
            Reason: command.Reason.ToString(),
            LocationStoreId: command.RemoteStoreId,
            Notes: command.Notes);

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/Inventory/store/{command.RemoteStoreId}/product/{command.RemoteProductId}/stock")
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        message.Headers.Add("X-Deduplication-Key", deduplicationKey);

        var response = await _httpClient.SendAsync(message, cancellationToken);
        await PostVoidApiAsync(response, cancellationToken);
    }
}
