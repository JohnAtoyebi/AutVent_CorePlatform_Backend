namespace AutVent.CorePlatform.Api.Common;

public sealed class AppOptions
{
    public string PasswordResetBaseUrl { get; init; } = string.Empty;
    public string AutVentLoginUrl { get; init; } = string.Empty;
    public int PasswordResetExpiryMinutes { get; init; } = 30;
}
