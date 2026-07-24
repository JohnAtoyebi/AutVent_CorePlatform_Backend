using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Domain.Enums;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class InvoiceService(IUnitOfWork unitOfWork, INotificationService notificationService) : IInvoiceService
{
    private const decimal VatRate = 7.5m;
    private const string SystemActor = "system";

    public async Task<ApiResponse<InvoiceResponse>> CreateAsync(long storeId, long userId, CreateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var storeExists = await unitOfWork.Query<Store>()
            .AnyAsync(x => x.Id == storeId && !x.IsDeleted, cancellationToken);

        if (!storeExists)
            return NotFound("Store not found", "StoreNotFound", nameof(storeId));

        if (request.CustomerId.HasValue)
        {
            var customerExists = await unitOfWork.Query<Customer>()
                .AnyAsync(x => x.Id == request.CustomerId.Value && !x.IsDeleted, cancellationToken);

            if (!customerExists)
                return NotFound("Customer not found", "CustomerNotFound", nameof(request.CustomerId));
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await unitOfWork.Query<Product>()
            .Where(x => productIds.Contains(x.Id) && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Count)
            return ApiResponse<InvoiceResponse>.Failed(StatusCodes.Status400BadRequest, "One or more products not found",
                [new ApiError("ProductNotFound", "Some product IDs are invalid", nameof(request.Items))]);

        var invoiceNumber = await GenerateInvoiceNumberAsync(storeId, cancellationToken);

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            StoreId = storeId,
            CustomerId = request.CustomerId,
            IssueDate = request.IssueDate.ToUniversalTime(),
            DueDate = request.DueDate.ToUniversalTime(),
            PaymentTerms = request.PaymentTerms,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            PaymentMethod = request.PaymentMethod,
            VatRate = request.TaxAmount,
            Status = InvoiceStatus.Draft,
            Notes = request.Notes?.Trim(),
            CreatedBy = userId.ToString(),
            DateCreated = DateTime.UtcNow,
            IsActive = true
        };

        invoice.InvoiceItems = request.Items.Select(i => new InvoiceItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            Discount = i.Discount,
            LineTotal = (i.Quantity * i.UnitPrice) - i.Discount,
            CreatedBy = SystemActor,
            DateCreated = DateTime.UtcNow,
            IsActive = true
        }).ToList();

        ComputeTotals(invoice);

        await unitOfWork.CreateAsync(invoice, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<InvoiceResponse>.Created(MapToResponse(invoice, products), "Invoice created successfully");
    }

    public async Task<ApiResponse<InvoiceResponse>> GetByIdAsync(long storeId, long invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await unitOfWork.Query<Invoice>()
            .Include(x => x.Customer)
            .Include(x => x.InvoiceItems)
                .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == invoiceId && x.StoreId == storeId && !x.IsDeleted, cancellationToken);

        if (invoice is null)
            return NotFound("Invoice not found", "InvoiceNotFound", nameof(invoiceId));

        return ApiResponse<InvoiceResponse>.Ok(MapToResponse(invoice, null));
    }

    public async Task<ApiResponse<PagedResponse<InvoiceResponse>>> GetAllAsync(long storeId, PagedQueryRequest request, CancellationToken cancellationToken = default)
    {
        var query = unitOfWork.Query<Invoice>()
            .Include(x => x.Customer)
            .Include(x => x.InvoiceItems)
                .ThenInclude(x => x.Product)
            .Where(x => x.StoreId == storeId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.InvoiceNumber.ToLower().Contains(search) ||
                (x.Customer != null && x.Customer.FullName.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var invoices = await query
            .OrderByDescending(x => x.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = invoices.Select(i => MapToResponse(i, null)).ToList();

        var paged = new PagedResponse<InvoiceResponse>
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
            Items = items
        };

        return ApiResponse<PagedResponse<InvoiceResponse>>.Ok(paged);
    }

    public async Task<ApiResponse<InvoiceResponse>> UpdateAsync(long storeId, long invoiceId, long userId, UpdateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await unitOfWork.Query<Invoice>()
            .Include(x => x.InvoiceItems)
            .FirstOrDefaultAsync(x => x.Id == invoiceId && x.StoreId == storeId && !x.IsDeleted, cancellationToken);

        if (invoice is null)
            return NotFound("Invoice not found", "InvoiceNotFound", nameof(invoiceId));

        if (invoice.Status != InvoiceStatus.Draft)
            return ApiResponse<InvoiceResponse>.Failed(StatusCodes.Status400BadRequest,
                "Only draft invoices can be updated",
                [new ApiError("InvalidStatus", "Invoice must be in Draft status to edit", nameof(invoice.Status))]);

        if (request.CustomerId.HasValue)
        {
            var customerExists = await unitOfWork.Query<Customer>()
                .AnyAsync(x => x.Id == request.CustomerId.Value && !x.IsDeleted, cancellationToken);

            if (!customerExists)
                return NotFound("Customer not found", "CustomerNotFound", nameof(request.CustomerId));
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await unitOfWork.Query<Product>()
            .Where(x => productIds.Contains(x.Id) && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Count)
            return ApiResponse<InvoiceResponse>.Failed(StatusCodes.Status400BadRequest, "One or more products not found",
                [new ApiError("ProductNotFound", "Some product IDs are invalid", nameof(request.Items))]);

        invoice.CustomerId = request.CustomerId;
        invoice.IssueDate = request.IssueDate.ToUniversalTime();
        invoice.DueDate = request.DueDate.ToUniversalTime();
        invoice.PaymentTerms = request.PaymentTerms;
        invoice.DiscountType = request.DiscountType;
        invoice.DiscountValue = request.DiscountValue;
        invoice.PaymentMethod = request.PaymentMethod;
        invoice.VatRate = request.TaxAmount;
        invoice.Notes = request.Notes?.Trim();
        invoice.UpdatedBy = userId.ToString();
        invoice.DateUpdated = DateTime.UtcNow;

        foreach (var item in invoice.InvoiceItems.ToList())
            unitOfWork.Delete(item);

        invoice.InvoiceItems = request.Items.Select(i => new InvoiceItem
        {
            InvoiceId = invoice.Id,
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            Discount = i.Discount,
            LineTotal = (i.Quantity * i.UnitPrice) - i.Discount,
            CreatedBy = SystemActor,
            DateCreated = DateTime.UtcNow,
            IsActive = true
        }).ToList();

        ComputeTotals(invoice);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<InvoiceResponse>.Ok(MapToResponse(invoice, products));
    }

    public async Task<ApiResponse<InvoiceResponse>> MarkAsSentAsync(long storeId, long invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await unitOfWork.Query<Invoice>()
            .Include(x => x.Customer)
            .Include(x => x.InvoiceItems).ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == invoiceId && x.StoreId == storeId && !x.IsDeleted, cancellationToken);

        if (invoice is null)
            return NotFound("Invoice not found", "InvoiceNotFound", nameof(invoiceId));

        if (invoice.Status != InvoiceStatus.Draft)
            return ApiResponse<InvoiceResponse>.Failed(StatusCodes.Status400BadRequest,
                "Only draft invoices can be marked as sent",
                [new ApiError("InvalidStatus", "Invoice is not in Draft status", nameof(invoice.Status))]);

        invoice.Status = InvoiceStatus.Sent;
        invoice.DateUpdated = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var ownerId = await GetStoreOwnerIdAsync(storeId, cancellationToken);
        if (ownerId.HasValue)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest
            {
                UserId = ownerId.Value,
                Type = NotificationType.InvoiceSent,
                Title = "Invoice sent",
                Message = $"Invoice {invoice.InvoiceNumber} has been marked as sent.",
                ActionUrl = $"/invoices/{invoice.Id}"
            }, cancellationToken);
        }

        return ApiResponse<InvoiceResponse>.Ok(MapToResponse(invoice, null));
    }

    public async Task<ApiResponse<InvoiceResponse>> RecordPaymentAsync(long storeId, long invoiceId, RecordInvoicePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await unitOfWork.Query<Invoice>()
            .Include(x => x.Customer)
            .Include(x => x.InvoiceItems).ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == invoiceId && x.StoreId == storeId && !x.IsDeleted, cancellationToken);

        if (invoice is null)
            return NotFound("Invoice not found", "InvoiceNotFound", nameof(invoiceId));

        if (invoice.Status is InvoiceStatus.Paid or InvoiceStatus.Cancelled)
            return ApiResponse<InvoiceResponse>.Failed(StatusCodes.Status400BadRequest,
                "Cannot record payment for a paid or cancelled invoice",
                [new ApiError("InvalidStatus", "Invoice is already paid or cancelled", nameof(invoice.Status))]);

        if (request.AmountPaid > invoice.BalanceRemaining)
            return ApiResponse<InvoiceResponse>.Failed(StatusCodes.Status400BadRequest,
                "Amount paid exceeds the remaining balance",
                [new ApiError("OverPayment", "Payment exceeds balance remaining", nameof(request.AmountPaid))]);

        invoice.AmountPaid += request.AmountPaid;
        invoice.BalanceRemaining = invoice.TotalAmount - invoice.AmountPaid;
        invoice.PaymentMethod = request.PaymentMethod;
        invoice.Status = invoice.BalanceRemaining == 0 ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
        invoice.DateUpdated = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var ownerId = await GetStoreOwnerIdAsync(storeId, cancellationToken);
        if (ownerId.HasValue)
        {
            var isPaid = invoice.Status == InvoiceStatus.Paid;
            await notificationService.CreateAsync(new CreateNotificationRequest
            {
                UserId = ownerId.Value,
                Type = isPaid ? NotificationType.InvoicePaid : NotificationType.InvoiceSent,
                Title = isPaid ? "Invoice fully paid" : "Invoice partial payment received",
                Message = isPaid
                    ? $"Invoice {invoice.InvoiceNumber} has been fully paid (#{invoice.TotalAmount:N2})."
                    : $"Invoice {invoice.InvoiceNumber}: #{request.AmountPaid:N2} received. Balance remaining: #{invoice.BalanceRemaining:N2}.",
                ActionUrl = $"/invoices/{invoice.Id}"
            }, cancellationToken);
        }

        return ApiResponse<InvoiceResponse>.Ok(MapToResponse(invoice, null));
    }

    public async Task<ApiResponse<InvoiceResponse>> CancelAsync(long storeId, long invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await unitOfWork.Query<Invoice>()
            .Include(x => x.Customer)
            .Include(x => x.InvoiceItems).ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == invoiceId && x.StoreId == storeId && !x.IsDeleted, cancellationToken);

        if (invoice is null)
            return NotFound("Invoice not found", "InvoiceNotFound", nameof(invoiceId));

        if (invoice.Status is InvoiceStatus.Paid)
            return ApiResponse<InvoiceResponse>.Failed(StatusCodes.Status400BadRequest,
                "Paid invoices cannot be cancelled",
                [new ApiError("InvalidStatus", "Invoice is already paid", nameof(invoice.Status))]);

        invoice.Status = InvoiceStatus.Cancelled;
        invoice.DateUpdated = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<InvoiceResponse>.Ok(MapToResponse(invoice, null));
    }

    public async Task<ApiResponse<bool>> DeleteAsync(long storeId, long invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await unitOfWork.Query<Invoice>()
            .FirstOrDefaultAsync(x => x.Id == invoiceId && x.StoreId == storeId && !x.IsDeleted, cancellationToken);

        if (invoice is null)
            return ApiResponse<bool>.Failed(StatusCodes.Status404NotFound,
                "Invoice not found",
                [new ApiError("InvoiceNotFound", "No invoice found for this id", nameof(invoiceId))]);

        if (invoice.Status is InvoiceStatus.Paid)
            return ApiResponse<bool>.Failed(StatusCodes.Status400BadRequest,
                "Paid invoices cannot be deleted",
                [new ApiError("InvalidStatus", "Invoice is paid and cannot be deleted", nameof(invoice.Status))]);

        invoice.IsDeleted = true;
        invoice.DateUpdated = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Invoice deleted successfully");
    }

    private static void ComputeTotals(Invoice invoice)
    {
        var subTotal = invoice.InvoiceItems.Sum(i => i.LineTotal);
        invoice.SubTotal = subTotal;

        invoice.DiscountAmount = invoice.DiscountType switch
        {
            SaleDiscountType.Percentage => Math.Round(subTotal * invoice.DiscountValue / 100, 2),
            SaleDiscountType.Amount => invoice.DiscountValue,
            _ => 0
        };

        var afterDiscount = subTotal - invoice.DiscountAmount;
        invoice.VatAmount = Math.Round(afterDiscount * invoice.VatRate / 100, 2);
        invoice.TotalAmount = afterDiscount + invoice.VatAmount;
        invoice.BalanceRemaining = invoice.TotalAmount - invoice.AmountPaid;
    }

    private async Task<string> GenerateInvoiceNumberAsync(long storeId, CancellationToken cancellationToken)
    {
        var count = await unitOfWork.Query<Invoice>()
            .CountAsync(x => x.StoreId == storeId, cancellationToken);

        return $"INV-{storeId:D4}-{(count + 1):D5}";
    }

    private async Task<long?> GetStoreOwnerIdAsync(long storeId, CancellationToken cancellationToken)
    {
        var store = await unitOfWork.Query<Store>()
            .Include(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == storeId, cancellationToken);

        return store?.Business?.UserId;
    }

    private static InvoiceResponse MapToResponse(Invoice invoice, List<Product>? products)
    {
        return new InvoiceResponse
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            StoreId = invoice.StoreId,
            CustomerId = invoice.CustomerId,
            CustomerName = invoice.Customer?.FullName,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            PaymentTerms = invoice.PaymentTerms,
            SubTotal = invoice.SubTotal,
            DiscountType = invoice.DiscountType,
            DiscountValue = invoice.DiscountValue,
            DiscountAmount = invoice.DiscountAmount,
            VatRate = invoice.VatRate,
            VatAmount = invoice.VatAmount,
            TotalAmount = invoice.TotalAmount,
            AmountPaid = invoice.AmountPaid,
            BalanceRemaining = invoice.BalanceRemaining,
            PaymentMethod = invoice.PaymentMethod,
            Status = invoice.Status,
            Notes = invoice.Notes,
            CreatedAt = invoice.DateCreated,
            Items = invoice.InvoiceItems.Select(i => new InvoiceItemResponse
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? products?.FirstOrDefault(p => p.Id == i.ProductId)?.Name ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Discount = i.Discount,
                LineTotal = i.LineTotal
            }).ToList()
        };
    }

    private static ApiResponse<InvoiceResponse> NotFound(string message, string code, string field) =>
        ApiResponse<InvoiceResponse>.Failed(StatusCodes.Status404NotFound, message,
            [new ApiError(code, message, field)]);
}
