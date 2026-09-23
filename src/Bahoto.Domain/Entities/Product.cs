using Bahoto.Domain.Common;

namespace Bahoto.Domain.Entities;

public class Product : BaseEntity
{
    public string Brand { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
