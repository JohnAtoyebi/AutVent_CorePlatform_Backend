using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AutVent.Infrastructure.Data;

/// <summary>
/// Used only by the EF Core tooling (dotnet ef migrations) — not registered in the DI container.
/// Points at a temp file so the tools can inspect the schema without a live app host.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AutVentDbContext>
{
    public AutVentDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AutVentDbContext>()
            .UseSqlite("Data Source=autvent_design_time.db")
            .Options;

        return new AutVentDbContext(options);
    }
}
