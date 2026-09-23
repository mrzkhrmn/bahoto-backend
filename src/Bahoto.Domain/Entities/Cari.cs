using Bahoto.Domain.Common;

namespace Bahoto.Domain.Entities;

public class Cari : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal IncomingAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
}
