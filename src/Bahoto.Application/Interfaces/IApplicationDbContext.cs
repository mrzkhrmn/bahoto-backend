using Bahoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bahoto.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<OilChange> OilChanges { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
