using AutVent.Application.Abstractions.Persistence;
using AutVent.Domain.Entities;
using AutVent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutVent.Infrastructure.Persistence;

public sealed class SettingRepository : ISettingRepository
{
    private readonly AutVentDbContext _dbContext;

    public SettingRepository(AutVentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task UpsertAsync(IEnumerable<AppSetting> settings, CancellationToken cancellationToken)
    {
        foreach (var setting in settings)
        {
            var existing = await _dbContext.Settings.FirstOrDefaultAsync(x => x.Key == setting.Key, cancellationToken);
            if (existing is null)
            {
                _dbContext.Settings.Add(setting);
                continue;
            }

            existing.Value = setting.Value;
            existing.UpdatedAtUtc = setting.UpdatedAtUtc;
        }
    }
}
