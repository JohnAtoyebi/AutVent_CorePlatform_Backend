using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Infrastructure.Persistence;

public sealed class CorePlatformDbContext(DbContextOptions<CorePlatformDbContext> options) : DbContext(options)
{
}
