using Microsoft.AspNetCore.Http;

namespace AutVent.CorePlatform.Api.Services;

public sealed class AccessContext(IHttpContextAccessor httpContextAccessor) : IAccessContext
{
    public bool IsPlatformAdmin => httpContextAccessor.HttpContext?.User?.IsInRole("BackOfficeAdmin") == true;
}
