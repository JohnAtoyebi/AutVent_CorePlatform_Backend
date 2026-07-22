namespace AutVent.Application.Abstractions.Services;

public interface ISyncCoordinator
{
    Task RunCycleAsync(CancellationToken cancellationToken);
}
