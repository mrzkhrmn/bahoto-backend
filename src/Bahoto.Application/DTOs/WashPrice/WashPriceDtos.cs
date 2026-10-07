namespace Bahoto.Application.DTOs.WashPrice;

public class WashPriceDto
{
    public Guid Id { get; set; }
    public string BrandModel { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime? LastPriceDate { get; set; }
    public decimal? CardInteriorExterior { get; set; }
    public decimal? CashInteriorExterior { get; set; }
    public decimal? CardExterior { get; set; }
    public decimal? CashExterior { get; set; }
    public decimal? CardUnderWash { get; set; }
    public decimal? CashUnderWash { get; set; }
    public decimal? CardUnderEngine { get; set; }
    public decimal? CashUnderEngine { get; set; }
    public decimal? CardUnderOverEngine { get; set; }
    public decimal? CashUnderOverEngine { get; set; }
    public decimal? CardOverEngine { get; set; }
    public decimal? CashOverEngine { get; set; }
    public decimal? CardUnderWashEngine { get; set; }
    public decimal? CashUnderWashEngine { get; set; }
    public decimal? CardFullWash { get; set; }
    public decimal? CashFullWash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class WashPriceListData
{
    public List<WashPriceDto> Items { get; set; } = [];
    public DateTime? TableLastPriceDate { get; set; }
}

public class CreateWashPriceRequest
{
    public string BrandModel { get; set; } = string.Empty;
    public decimal? CardInteriorExterior { get; set; }
    public decimal? CashInteriorExterior { get; set; }
    public decimal? CardExterior { get; set; }
    public decimal? CashExterior { get; set; }
    public decimal? CardUnderWash { get; set; }
    public decimal? CashUnderWash { get; set; }
    public decimal? CardUnderEngine { get; set; }
    public decimal? CashUnderEngine { get; set; }
    public decimal? CardUnderOverEngine { get; set; }
    public decimal? CashUnderOverEngine { get; set; }
    public decimal? CardOverEngine { get; set; }
    public decimal? CashOverEngine { get; set; }
    public decimal? CardUnderWashEngine { get; set; }
    public decimal? CashUnderWashEngine { get; set; }
    public decimal? CardFullWash { get; set; }
    public decimal? CashFullWash { get; set; }
}

public class UpdateWashPriceRequest : CreateWashPriceRequest
{
    public Guid Id { get; set; }
}

public class DeleteWashPriceRequest
{
    public Guid Id { get; set; }
}

public class ReorderWashPriceRequest
{
    public List<Guid> Ids { get; set; } = [];
}

public class ApplyWashPriceIncreaseRequest
{
    /// <summary>all | service</summary>
    public string Scope { get; set; } = "all";
    public string? ServiceKey { get; set; }
    public decimal Percent { get; set; }
}

public class ApplyServicePriceIncreaseResultDto
{
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
}
