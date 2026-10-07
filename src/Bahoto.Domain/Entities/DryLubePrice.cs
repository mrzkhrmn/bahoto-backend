using Bahoto.Domain.Common;

namespace Bahoto.Domain.Entities;

public class DryLubePrice : BaseEntity
{
    public string BrandModel { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime? LastPriceDate { get; set; }

    public decimal? CardNormal { get; set; }
    public decimal? CashNormal { get; set; }
    public decimal? CardWaterless { get; set; }
    public decimal? CashWaterless { get; set; }
    public decimal? CardUnderWashDryLube { get; set; }
    public decimal? CashUnderWashDryLube { get; set; }
}
