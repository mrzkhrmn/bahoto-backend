using Bahoto.Domain.Common;

namespace Bahoto.Domain.Entities;

public class WashPrice : BaseEntity
{
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
}
