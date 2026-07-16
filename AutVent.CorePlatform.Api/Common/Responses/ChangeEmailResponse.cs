namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class ChangeEmailResponse
{
    public long UserId { get; init; }
    public string NewEmailAddress { get; init; } = string.Empty;
}
