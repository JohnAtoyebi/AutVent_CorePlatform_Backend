namespace AutVent.CorePlatform.Api.Common.Email;

public sealed class EmailOptions
{
    public string Provider { get; init; } = "Resend";
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;
    public string SupportEmail { get; init; } = string.Empty;
    public ResendOptions Resend { get; init; } = new();
}

public sealed class ResendOptions
{
    public string ApiKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://api.resend.com";
    public ResendTemplates Templates { get; init; } = new();
}

public sealed class ResendTemplates
{
    public string OtpVerification { get; init; } = string.Empty;
    public string PasswordReset { get; init; } = string.Empty;
    public string ForgotPassword { get; init; } = string.Empty;
    public string BusinessWelcome { get; init; } = string.Empty;
    public string ContactSupport { get; init; } = string.Empty;
}
