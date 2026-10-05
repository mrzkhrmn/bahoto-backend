using Bahoto.Domain.Common;

namespace Bahoto.Domain.Entities;

public class Cari : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    /// <summary>Adet veya Koli</summary>
    public string QuantityUnit { get; set; } = "Adet";
    public decimal IncomingAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
}
