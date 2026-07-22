using AutVent.Application.Abstractions.Security;

namespace AutVent.Infrastructure.Security;

public sealed class InMemoryTokenStore : ITokenStore
{
    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }
}
