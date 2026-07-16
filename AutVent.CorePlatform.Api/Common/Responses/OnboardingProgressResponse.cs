namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class OnboardingProgressResponse
{
    public bool AccountCreated { get; init; }
    public bool EmailVerified { get; init; }
    public bool BusinessCreated { get; init; }
    public bool StoreCreated { get; init; }
    public bool ProductAdded { get; init; }
    public int CompletedSteps { get; init; }
    public int TotalSteps { get; init; }
    public int ProgressPercent { get; init; }
}
