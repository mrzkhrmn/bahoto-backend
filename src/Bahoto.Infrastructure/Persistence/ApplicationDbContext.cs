using Bahoto.Application.Interfaces;
using Bahoto.Domain.Common;
using Bahoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bahoto.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<OilChange> OilChanges => Set<OilChange>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<BrandOrder> BrandOrders => Set<BrandOrder>();
    public DbSet<Cari> Caris => Set<Cari>();
    public DbSet<WashPrice> WashPrices => Set<WashPrice>();
    public DbSet<DryLubePrice> DryLubePrices => Set<DryLubePrice>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasQueryFilter(x => x.DeletedAt == null);
        });

        modelBuilder.Entity<OilChange>(entity =>
        {
            entity.ToTable("OilChanges");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Vehicle).HasMaxLength(200);
            entity.Property(x => x.Plate).HasMaxLength(20);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.OilType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.OilFilter).HasMaxLength(200);
            entity.Property(x => x.AirFilter).HasMaxLength(200);
            entity.Property(x => x.FuelFilter).HasMaxLength(200);
            entity.Property(x => x.PolenFilter).HasMaxLength(200);
            entity.Property(x => x.Note).HasMaxLength(2000);
            entity.Property(x => x.Employee).HasMaxLength(200);
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.HasQueryFilter(x => x.DeletedAt == null);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Brand).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.Brand, x.Name })
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL");
            entity.HasQueryFilter(x => x.DeletedAt == null);
        });

        modelBuilder.Entity<BrandOrder>(entity =>
        {
            entity.ToTable("BrandOrders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Brand).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.Brand)
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL");
            entity.HasIndex(x => x.SortOrder);
            entity.HasQueryFilter(x => x.DeletedAt == null);
        });

        modelBuilder.Entity<Cari>(entity =>
        {
            entity.ToTable("Caris");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.QuantityUnit).HasMaxLength(20).IsRequired();
            entity.Property(x => x.IncomingAmount).HasPrecision(18, 2);
            entity.Property(x => x.PaidAmount).HasPrecision(18, 2);
            entity.Property(x => x.Balance).HasPrecision(18, 2);
            entity.HasIndex(x => x.ProductId);
            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => x.DeletedAt == null);
        });

        modelBuilder.Entity<WashPrice>(entity =>
        {
            entity.ToTable("WashPrices");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.BrandModel).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CardInteriorExterior).HasPrecision(18, 2);
            entity.Property(x => x.CashInteriorExterior).HasPrecision(18, 2);
            entity.Property(x => x.CardExterior).HasPrecision(18, 2);
            entity.Property(x => x.CashExterior).HasPrecision(18, 2);
            entity.Property(x => x.CardUnderWash).HasPrecision(18, 2);
            entity.Property(x => x.CashUnderWash).HasPrecision(18, 2);
            entity.Property(x => x.CardUnderEngine).HasPrecision(18, 2);
            entity.Property(x => x.CashUnderEngine).HasPrecision(18, 2);
            entity.Property(x => x.CardUnderOverEngine).HasPrecision(18, 2);
            entity.Property(x => x.CashUnderOverEngine).HasPrecision(18, 2);
            entity.Property(x => x.CardOverEngine).HasPrecision(18, 2);
            entity.Property(x => x.CashOverEngine).HasPrecision(18, 2);
            entity.Property(x => x.CardUnderWashEngine).HasPrecision(18, 2);
            entity.Property(x => x.CashUnderWashEngine).HasPrecision(18, 2);
            entity.Property(x => x.CardFullWash).HasPrecision(18, 2);
            entity.Property(x => x.CashFullWash).HasPrecision(18, 2);
            entity.HasIndex(x => x.BrandModel)
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL");
            entity.HasIndex(x => x.SortOrder);
            entity.HasQueryFilter(x => x.DeletedAt == null);
        });

        modelBuilder.Entity<DryLubePrice>(entity =>
        {
            entity.ToTable("DryLubePrices");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.BrandModel).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CardNormal).HasPrecision(18, 2);
            entity.Property(x => x.CashNormal).HasPrecision(18, 2);
            entity.Property(x => x.CardWaterless).HasPrecision(18, 2);
            entity.Property(x => x.CashWaterless).HasPrecision(18, 2);
            entity.Property(x => x.CardUnderWashDryLube).HasPrecision(18, 2);
            entity.Property(x => x.CashUnderWashDryLube).HasPrecision(18, 2);
            entity.HasIndex(x => x.BrandModel)
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL");
            entity.HasIndex(x => x.SortOrder);
            entity.HasQueryFilter(x => x.DeletedAt == null);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => x.UserId);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(x => x.DeletedAt == null);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt ??= utcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
