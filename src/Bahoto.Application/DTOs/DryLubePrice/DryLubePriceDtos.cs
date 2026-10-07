namespace Bahoto.Application.DTOs.DryLubePrice;

public class DryLubePriceDto
{
    public Guid Id { get; set; }
    public string BrandModel { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime? LastPriceDate { get; set; }
    public decimal? CardNormal { get; set; }
    public decimal? CashNormal { get; set; }
    public decimal? CardWaterless { get; set; }
    public decimal? CashWaterless { get; set; }
    public decimal? CardUnderWashDryLube { get; set; }
    public decimal? CashUnderWashDryLube { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class DryLubePriceListData
{
    public List<DryLubePriceDto> Items { get; set; } = [];
    public DateTime? TableLastPriceDate { get; set; }
}

public class CreateDryLubePriceRequest
{
    public string BrandModel { get; set; } = string.Empty;
    public decimal? CardNormal { get; set; }
    public decimal? CashNormal { get; set; }
    public decimal? CardWaterless { get; set; }
    public decimal? CashWaterless { get; set; }
    public decimal? CardUnderWashDryLube { get; set; }
    public decimal? CashUnderWashDryLube { get; set; }
}

public class UpdateDryLubePriceRequest : CreateDryLubePriceRequest
{
    public Guid Id { get; set; }
}

public class DeleteDryLubePriceRequest
{
    public Guid Id { get; set; }
}

public class ReorderDryLubePriceRequest
{
    public List<Guid> Ids { get; set; } = [];
}

public class ApplyDryLubePriceIncreaseRequest
{
    /// <summary>all | service</summary>
    public string Scope { get; set; } = "all";
    public string? ServiceKey { get; set; }
    public decimal Percent { get; set; }
}

public class ApplyDryLubePriceIncreaseResultDto
{
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
}
