using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AutVent.CorePlatform.Api.Infrastructure;

/// <summary>
/// Removes the global Bearer security requirement from operations that are
/// decorated with <see cref="AllowAnonymousAttribute"/>, so the OpenAPI spec
/// correctly reflects which endpoints are publicly accessible.
/// </summary>
public sealed class AnonymousOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAllowAnonymous = context.MethodInfo
            .GetCustomAttributes(inherit: true)
            .OfType<AllowAnonymousAttribute>()
            .Any()
            || (context.MethodInfo.DeclaringType?
                .GetCustomAttributes(inherit: true)
                .OfType<AllowAnonymousAttribute>()
                .Any() ?? false);

        if (!hasAllowAnonymous)
            return;

        operation.Security = [new OpenApiSecurityRequirement()];
    }
}
