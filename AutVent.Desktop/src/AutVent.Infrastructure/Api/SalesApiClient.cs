using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AutVent.Application.Abstractions.Api;
using AutVent.Application.Contracts;

namespace AutVent.Infrastructure.Api;

// ── Internal API shapes ──────────────────────────────────────────────────────

file sealed record CreateSaleItemRequestBody(
    [property: JsonPropertyName("productId")] long ProductId,
    [property: JsonPropertyName("quantity")] long Quantity,
    [property: JsonPropertyName("unitPrice")] decimal UnitPrice);

file sealed record CreateSaleRequestBody(
    [property: JsonPropertyName("amountPaid")] decimal AmountPaid,
    [property: JsonPropertyName("paymentMethod")] string PaymentMethod,
    [property: JsonPropertyName("discountType")] string? DiscountType,
    [property: JsonPropertyName("discountValue")] decimal DiscountValue,
    [property: JsonPropertyName("taxAmount")] decimal TaxAmount,
    [property: JsonPropertyName("notes")] string? Notes,
    [property: JsonPropertyName("customerId")] long? CustomerId,
    [property: JsonPropertyName("balanceDueDate")] DateTime? BalanceDueDate,
    [property: JsonPropertyName("expectedBalanceRemaining")] decimal? ExpectedBalanceRemaining,
    [property: JsonPropertyName("items")] List<CreateSaleItemRequestBody> Items);

file sealed record SaleApiResponse(
    [property: JsonPropertyName("saleId")] long SaleId,
    [property: JsonPropertyName("saleNumber")] string SaleNumber,
    [property: JsonPropertyName("totalAmount")] decimal TotalAmount,
    [property: JsonPropertyName("changeAmount")] decimal ChangeAmount,
    [property: JsonPropertyName("status")] string Status);

// ── Client ───────────────────────────────────────────────────────────────────

public sealed class SalesApiClient : ApiClientBase, ISalesApiClient
{
    private readonly HttpClient _httpClient;

    public SalesApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CompletedSaleDto> CheckoutAsync(CompleteSaleCommand command, CancellationToken cancellationToken)
    {
        var body = new CreateSaleRequestBody(
            AmountPaid: command.AmountPaid,
            PaymentMethod: command.PaymentMethod.ToString(),
            DiscountType: command.DiscountValue > 0 ? command.DiscountType.ToString() : null,
            DiscountValue: command.DiscountValue,
            TaxAmount: command.TaxAmount,
            Notes: command.Notes,
            CustomerId: command.RemoteCustomerId,
            BalanceDueDate: command.BalanceDueDate,
            ExpectedBalanceRemaining: command.ExpectedBalanceRemaining,
            Items: command.Lines.Select(l => new CreateSaleItemRequestBody(
                ProductId: l.RemoteProductId,
                Quantity: (long)l.Quantity,
                UnitPrice: l.UnitPrice)).ToList());

        var response = await _httpClient.PostAsJsonAsync(
            $"api/Pos/store/{command.RemoteStoreId}/checkout", body, JsonOptions, cancellationToken);
        var data = await ReadApiResponseAsync<SaleApiResponse>(response, cancellationToken);

        return new CompletedSaleDto(
            RemoteId: data.SaleId,
            SaleNumber: data.SaleNumber,
            TotalAmount: data.TotalAmount,
            ChangeAmount: data.ChangeAmount,
            Status: data.Status);
    }
}
