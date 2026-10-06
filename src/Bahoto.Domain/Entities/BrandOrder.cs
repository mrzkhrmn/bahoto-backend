using Bahoto.Domain.Common;

namespace Bahoto.Domain.Entities;

public class BrandOrder : BaseEntity
{
    public string Brand { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
