using AutVent.CorePlatform.Api.Common.Requests;
using AutVent.CorePlatform.Api.Common.Responses;

namespace AutVent.CorePlatform.Api.Services;

public interface ISupportService
{
    Task<ApiResponse<string>> ContactAsync(ContactSupportRequest request, CancellationToken cancellationToken = default);
}
