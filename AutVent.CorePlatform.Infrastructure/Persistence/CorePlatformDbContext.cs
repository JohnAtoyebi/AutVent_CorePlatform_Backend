using AutVent.CorePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Infrastructure.Persistence;

public sealed class CorePlatformDbContext(DbContextOptions<CorePlatformDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Otp> Otps => Set<Otp>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessIndustry> BusinessIndustries => Set<BusinessIndustry>();
    public DbSet<StaffRange> StaffRanges => Set<StaffRange>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<StoreCategory> StoreCategories => Set<StoreCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<ReferralRecord> ReferralRecords => Set<ReferralRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EmailAddress).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Password).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ReferralCode).HasMaxLength(50);
            entity.HasIndex(x => x.EmailAddress).IsUnique();
            entity.HasIndex(x => x.PhoneNumber).IsUnique();
            entity.HasIndex(x => x.ReferralCode).IsUnique();
        });

        modelBuilder.Entity<Otp>(entity =>
        {
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.EmailAddress).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<Business>(entity =>
        {
            entity.Property(x => x.BusinessName).HasMaxLength(200).IsRequired();

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.BusinessIndustry)
                .WithMany()
                .HasForeignKey(x => x.BusinessIndustryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.StaffRange)
                .WithMany(x => x.Businesses)
                .HasForeignKey(x => x.StaffRangeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StaffRange>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<BusinessIndustry>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EmailAddress).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();

            entity.HasOne(x => x.StoreCategory)
                .WithMany()
                .HasForeignKey(x => x.StoreCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Business)
                .WithMany(x => x.Stores)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoreCategory>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Price).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.Sku).HasMaxLength(100);
            entity.Property(x => x.Barcode).HasMaxLength(100);
            entity.Property(x => x.CostPrice).HasMaxLength(50);
            entity.Property(x => x.CompareAtPrice).HasMaxLength(50);
            entity.Property(x => x.ProductImagesJson);
            entity.Property(x => x.ProductVariantsJson);
            entity.Property(x => x.TagsJson);
            entity.Property(x => x.Supplier).HasMaxLength(200);

            entity.HasOne(x => x.ProductCategory)
                .WithMany()
                .HasForeignKey(x => x.ProductCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.Property(x => x.Token).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => x.Token).IsUnique();

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200);
            entity.HasIndex(x => new { x.PhoneNumber, x.StoreId }).IsUnique();
            entity.HasIndex(x => new { x.Email, x.StoreId }).IsUnique();

            entity.HasOne(x => x.Store)
                .WithMany()
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.Property(x => x.SaleNumber).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.DiscountType).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasIndex(x => x.SaleNumber).IsUnique();

            entity.HasOne(x => x.Store)
                .WithMany()
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.HasOne(x => x.Sale)
                .WithMany(x => x.SaleItems)
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureBaseEntityProperties(modelBuilder);

        modelBuilder.Entity<ReferralRecord>(entity =>
        {
            entity.Property(x => x.ReferralCode).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => new { x.ReferrerId, x.ReferredUserId }).IsUnique();

            entity.HasOne(x => x.Referrer)
                .WithMany()
                .HasForeignKey(x => x.ReferrerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ReferredUser)
                .WithMany()
                .HasForeignKey(x => x.ReferredUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureBaseEntityProperties(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(x => typeof(BaseEntity).IsAssignableFrom(x.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType).Property(nameof(BaseEntity.CreatedBy)).HasMaxLength(200).IsRequired();
            modelBuilder.Entity(entityType.ClrType).Property(nameof(BaseEntity.UpdatedBy)).HasMaxLength(200);
            modelBuilder.Entity(entityType.ClrType).Property(nameof(BaseEntity.DateCreated)).IsRequired();
        }
    }
}
