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
