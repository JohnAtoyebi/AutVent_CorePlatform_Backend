using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IInvoiceService
{
    Task<ApiResponse<InvoiceResponse>> CreateAsync(long storeId, long userId, CreateInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<InvoiceResponse>> GetByIdAsync(long storeId, long invoiceId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<InvoiceResponse>>> GetAllAsync(long storeId, PagedQueryRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<InvoiceResponse>> UpdateAsync(long storeId, long invoiceId, long userId, UpdateInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<InvoiceResponse>> MarkAsSentAsync(long storeId, long invoiceId, CancellationToken cancellationToken = default);
    Task<ApiResponse<InvoiceResponse>> RecordPaymentAsync(long storeId, long invoiceId, RecordInvoicePaymentRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<InvoiceResponse>> CancelAsync(long storeId, long invoiceId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(long storeId, long invoiceId, CancellationToken cancellationToken = default);
}
