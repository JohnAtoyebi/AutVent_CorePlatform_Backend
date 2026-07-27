namespace AutVent.Application.Abstractions.Security;

/// <summary>In-memory bearer token cache used by the HTTP pipeline to attach authorization headers.</summary>
public interface ITokenStore
{
    string? AccessToken { get; set; }

    string? RefreshToken { get; set; }
}
