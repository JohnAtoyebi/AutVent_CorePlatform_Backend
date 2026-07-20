using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Domain.Entities;
using AutVent.CorePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public sealed class BankAccountService(IUnitOfWork unitOfWork) : IBankAccountService
{
    private const string SystemActor = "system";

    public async Task<ApiResponse<IList<BankAccountResponse>>> GetAllAsync(long userId, CancellationToken cancellationToken = default)
    {
        var business = await ResolveBusinessAsync(userId, cancellationToken);
        if (business is null)
            return ApiResponse<IList<BankAccountResponse>>.Failed(StatusCodes.Status404NotFound, "Business not found",
                [new ApiError("BusinessNotFound", "Create a business before managing bank accounts", "userId")]);

        var accounts = await unitOfWork.Query<BusinessBankAccount>()
            .Where(b => b.BusinessId == business.Id && !b.IsDeleted)
            .OrderByDescending(b => b.IsDefault)
            .ThenBy(b => b.Id)
            .Select(b => new BankAccountResponse
            {
                Id = b.Id,
                BankName = b.BankName,
                AccountNumber = b.AccountNumber,
                AccountName = b.AccountName,
                SortCode = b.SortCode,
                IsDefault = b.IsDefault
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IList<BankAccountResponse>>.Ok(accounts);
    }

    public async Task<ApiResponse<BankAccountResponse>> CreateAsync(CreateBankAccountRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var business = await ResolveBusinessAsync(userId, cancellationToken);
        if (business is null)
            return ApiResponse<BankAccountResponse>.Failed(StatusCodes.Status404NotFound, "Business not found",
                [new ApiError("BusinessNotFound", "Create a business before managing bank accounts", "userId")]);

        var now = DateTime.UtcNow;

        // If this is the first account or caller requests default, clear existing default
        if (request.IsDefault)
            await ClearDefaultAsync(business.Id, cancellationToken);

        var account = new BusinessBankAccount
        {
            BusinessId = business.Id,
            BankName = request.BankName.Trim(),
            AccountNumber = request.AccountNumber.Trim(),
            AccountName = request.AccountName.Trim(),
            SortCode = request.SortCode?.Trim(),
            IsDefault = request.IsDefault,
            IsActive = true,
            CreatedBy = SystemActor,
            DateCreated = now
        };

        // Auto-set default if it's the only account
        var hasAny = await unitOfWork.Query<BusinessBankAccount>()
            .AnyAsync(b => b.BusinessId == business.Id && !b.IsDeleted, cancellationToken);
        if (!hasAny) account.IsDefault = true;

        await unitOfWork.CreateAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<BankAccountResponse>.Created(Map(account), "Bank account added successfully");
    }

    public async Task<ApiResponse<BankAccountResponse>> UpdateAsync(long id, CreateBankAccountRequest request, long userId, CancellationToken cancellationToken = default)
    {
        var (account, err) = await ResolveAccountAsync<BankAccountResponse>(id, userId, cancellationToken);
        if (err is not null) return err;

        if (request.IsDefault && !account!.IsDefault)
            await ClearDefaultAsync(account.BusinessId, cancellationToken);

        account!.BankName = request.BankName.Trim();
        account.AccountNumber = request.AccountNumber.Trim();
        account.AccountName = request.AccountName.Trim();
        account.SortCode = request.SortCode?.Trim();
        account.IsDefault = request.IsDefault;
        account.UpdatedBy = SystemActor;
        account.DateUpdated = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApiResponse<BankAccountResponse>.Ok(Map(account), "Bank account updated successfully");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var (account, err) = await ResolveAccountAsync<bool>(id, userId, cancellationToken);
        if (err is not null) return err;

        unitOfWork.Delete(account!);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Auto-promote the oldest remaining account to default if the deleted one was default
        if (account!.IsDefault)
        {
            var next = await unitOfWork.Query<BusinessBankAccount>()
                .Where(b => b.BusinessId == account.BusinessId && !b.IsDeleted)
                .OrderBy(b => b.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (next is not null)
            {
                next.IsDefault = true;
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        return ApiResponse<bool>.Ok(true, "Bank account deleted successfully");
    }

    public async Task<ApiResponse<BankAccountResponse>> SetDefaultAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var (account, err) = await ResolveAccountAsync<BankAccountResponse>(id, userId, cancellationToken);
        if (err is not null) return err;

        await ClearDefaultAsync(account!.BusinessId, cancellationToken);
        account!.IsDefault = true;
        account.UpdatedBy = SystemActor;
        account.DateUpdated = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApiResponse<BankAccountResponse>.Ok(Map(account), "Default bank account updated");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<Business?> ResolveBusinessAsync(long userId, CancellationToken ct) =>
        await unitOfWork.Query<Business>().FirstOrDefaultAsync(b => b.UserId == userId, ct);

    private async Task<(BusinessBankAccount? account, ApiResponse<T>? error)> ResolveAccountAsync<T>(long id, long userId, CancellationToken ct)
    {
        var account = await unitOfWork.Query<BusinessBankAccount>()
            .Include(b => b.Business)
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, ct);

        if (account is null)
            return (null, ApiResponse<T>.Failed(StatusCodes.Status404NotFound, "Bank account not found",
                [new ApiError("NotFound", "Bank account not found", nameof(id))]));

        if (account.Business.UserId != userId)
            return (null, ApiResponse<T>.Failed(StatusCodes.Status403Forbidden, "Access denied",
                [new ApiError("Forbidden", "This bank account does not belong to your business", nameof(id))]));

        return (account, null);
    }

    private async Task ClearDefaultAsync(long businessId, CancellationToken ct)
    {
        var current = await unitOfWork.Query<BusinessBankAccount>()
            .Where(b => b.BusinessId == businessId && b.IsDefault && !b.IsDeleted)
            .ToListAsync(ct);

        foreach (var b in current) b.IsDefault = false;
    }

    private static BankAccountResponse Map(BusinessBankAccount b) => new()
    {
        Id = b.Id,
        BankName = b.BankName,
        AccountNumber = b.AccountNumber,
        AccountName = b.AccountName,
        SortCode = b.SortCode,
        IsDefault = b.IsDefault
    };
}
