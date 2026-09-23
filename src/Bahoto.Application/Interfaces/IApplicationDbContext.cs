using Bahoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bahoto.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<OilChange> OilChanges { get; }
    DbSet<Product> Products { get; }
    DbSet<Cari> Caris { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
