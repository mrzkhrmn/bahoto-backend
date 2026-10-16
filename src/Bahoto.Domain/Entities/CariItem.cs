using Bahoto.Domain.Common;

namespace Bahoto.Domain.Entities;

/// <summary>
/// Cari ekranına özel ürün kaydı. Fiyat tablosu (Product) ile bağlı değildir.
/// </summary>
public class CariItem : BaseEntity
{
    public string Brand { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
