using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface IBankAccountService
{
    Task<ApiResponse<IList<BankAccountResponse>>> GetAllAsync(long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<BankAccountResponse>> CreateAsync(CreateBankAccountRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<BankAccountResponse>> UpdateAsync(long id, CreateBankAccountRequest request, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(long id, long userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<BankAccountResponse>> SetDefaultAsync(long id, long userId, CancellationToken cancellationToken = default);
}
