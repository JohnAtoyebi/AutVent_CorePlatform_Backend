namespace AutVent.CorePlatform.Api.Common.Email;

public sealed class EmailMessage
{
    public string To { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string? TemplateAlias { get; init; }
    public Dictionary<string, object>? TemplateVariables { get; init; }
}
