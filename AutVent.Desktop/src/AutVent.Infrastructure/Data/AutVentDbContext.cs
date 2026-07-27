using AutVent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutVent.Infrastructure.Data;

public sealed class AutVentDbContext : DbContext
{
    public AutVentDbContext(DbContextOptions<AutVentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<InventoryRecord> Inventory => Set<InventoryRecord>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    public DbSet<Store> Stores => Set<Store>();

    public DbSet<AppSetting> Settings => Set<AppSetting>();

    public DbSet<PendingSyncOperation> PendingSync => Set<PendingSyncOperation>();

    public DbSet<AuthenticationSession> AuthenticationSessions => Set<AuthenticationSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasKey(x => x.Id);
        modelBuilder.Entity<Product>().HasIndex(x => x.Sku).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => x.RemoteId);

        modelBuilder.Entity<InventoryRecord>().HasKey(x => x.Id);
        modelBuilder.Entity<InventoryRecord>().HasIndex(x => new { x.StoreId, x.ProductId });
        modelBuilder.Entity<InventoryRecord>().HasIndex(x => new { x.RemoteStoreId, x.RemoteProductId }).IsUnique();

        modelBuilder.Entity<Sale>().HasKey(x => x.Id);
        modelBuilder.Entity<Sale>().HasMany(x => x.Items).WithOne().HasForeignKey(x => x.SaleId);

        modelBuilder.Entity<SaleItem>().HasKey(x => x.Id);

        modelBuilder.Entity<Store>().HasKey(x => x.Id);
        modelBuilder.Entity<Store>().HasIndex(x => x.RemoteId).IsUnique();

        modelBuilder.Entity<AppSetting>().HasKey(x => x.Id);
        modelBuilder.Entity<AppSetting>().HasIndex(x => x.Key).IsUnique();

        modelBuilder.Entity<PendingSyncOperation>().HasKey(x => x.Id);
        modelBuilder.Entity<PendingSyncOperation>().HasIndex(x => x.DeduplicationKey).IsUnique();

        modelBuilder.Entity<AuthenticationSession>().HasKey(x => x.Id);
    }
}
