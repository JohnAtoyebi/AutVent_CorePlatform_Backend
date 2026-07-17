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
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ReferralRecord> ReferralRecords => Set<ReferralRecord>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<StaffStoreAccess> StaffStoreAccess => Set<StaffStoreAccess>();
    public DbSet<SupportRequest> SupportRequests => Set<SupportRequest>();
    public DbSet<SubscriptionPlanDefinition> SubscriptionPlanDefinitions => Set<SubscriptionPlanDefinition>();
    public DbSet<BusinessSubscription> BusinessSubscriptions => Set<BusinessSubscription>();
    public DbSet<BillingSubscriptionTransaction> BillingSubscriptionTransactions => Set<BillingSubscriptionTransaction>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Notification> Notifications => Set<Notification>();

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

            entity.HasOne<Supplier>(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

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
            entity.Property(x => x.Address).HasMaxLength(500);
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

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.Property(x => x.InvoiceNumber).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PaymentTerms).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.DiscountType).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.VatRate).HasColumnType("decimal(5,2)");
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasIndex(x => x.InvoiceNumber).IsUnique();

            entity.HasOne(x => x.Store)
                .WithMany()
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasOne(x => x.Invoice)
                .WithMany(x => x.InvoiceItems)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureBaseEntityProperties(modelBuilder);

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.ActionUrl).HasMaxLength(500);
            entity.HasIndex(x => new { x.UserId, x.IsRead });

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Business)
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.SetNull);
        });

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

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(x => x.Token).HasMaxLength(200).IsRequired();

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockTransfer>(entity =>
        {
            entity.Property(x => x.TransferNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(500);

            entity.HasOne(x => x.SourceStore)
                .WithMany()
                .HasForeignKey(x => x.SourceStoreId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.DestinationStore)
                .WithMany()
                .HasForeignKey(x => x.DestinationStoreId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Items)
                .WithOne(x => x.StockTransfer)
                .HasForeignKey(x => x.StockTransferId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockTransferItem>(entity =>
        {
            entity.Property(x => x.Notes).HasMaxLength(500);

            entity.HasOne(x => x.SourceProduct)
                .WithMany()
                .HasForeignKey(x => x.SourceProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.DestinationProduct)
                .WithMany()
                .HasForeignKey(x => x.DestinationProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(300);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(300);
            entity.Property(x => x.Group).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasOne(x => x.Role)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Permission)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EmailAddress).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(20);
            entity.Property(x => x.Notes).HasMaxLength(500);

            entity.HasOne(x => x.Business)
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Role)
                .WithMany(x => x.StaffMembers)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.StoreAccess)
                .WithOne(x => x.Staff)
                .HasForeignKey(x => x.StaffId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StaffStoreAccess>(entity =>
        {
            entity.HasOne(x => x.Store)
                .WithMany()
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.StaffId, x.StoreId }).IsUnique();
        });

        modelBuilder.Entity<SupportRequest>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ContactEmail).HasMaxLength(200);
            entity.Property(x => x.ContactPhone).HasMaxLength(30);
        });

        modelBuilder.Entity<SubscriptionPlanDefinition>(entity =>
        {
            entity.Property(x => x.Plan).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.MonthlyPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.AnnualPrice).HasColumnType("decimal(18,2)");
            entity.HasIndex(x => x.Plan).IsUnique();
        });

        modelBuilder.Entity<BusinessSubscription>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();

            entity.HasOne(x => x.Business)
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.SubscriptionPlan)
                .WithMany()
                .HasForeignKey(x => x.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BillingSubscriptionTransaction>(entity =>
        {
            entity.Property(x => x.TransactionReference).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ProviderReference).HasMaxLength(100);
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(10).IsRequired();
            entity.Property(x => x.BillingCycle).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.VerificationStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(x => x.FailureReason).HasMaxLength(500);

            entity.HasIndex(x => x.TransactionReference).IsUnique();

            entity.HasOne(x => x.Business)
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.SubscriptionPlan)
                .WithMany()
                .HasForeignKey(x => x.SubscriptionPlanId)
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
