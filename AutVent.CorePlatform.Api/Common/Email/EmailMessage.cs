namespace AutVent.CorePlatform.Api.Common.Email;

public sealed class EmailMessage
{
    public string To { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string? TemplateAlias { get; init; }
    public Dictionary<string, object>? TemplateVariables { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public List<EmailAttachment>? Attachments { get; init; }
}

public sealed class EmailAttachment
{
    public string Filename { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty; // Base64 encoded content
    public string ContentType { get; init; } = "application/octet-stream";
}
