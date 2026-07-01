namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class ResetPasswordResponse
{
    public string EmailAddress { get; init; } = string.Empty;
    public bool PasswordReset { get; init; }
}
