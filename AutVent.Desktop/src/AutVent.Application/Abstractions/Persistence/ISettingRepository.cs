using AutVent.Domain.Entities;

namespace AutVent.Application.Abstractions.Persistence;

public interface ISettingRepository
{
    Task UpsertAsync(IEnumerable<AppSetting> settings, CancellationToken cancellationToken);
}
