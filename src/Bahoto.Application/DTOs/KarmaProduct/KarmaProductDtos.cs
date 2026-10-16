namespace Bahoto.Application.DTOs.KarmaProduct;

public class KarmaProductDto
{
    public Guid Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public DateTime? LastPriceDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class KarmaProductListData
{
    public List<KarmaProductDto> Items { get; set; } = [];
}

public class ListKarmaProductRequest
{
    public string? Search { get; set; }
}

public class CreateKarmaProductRequest
{
    public string Brand { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Price { get; set; }
}

public class UpdateKarmaProductRequest
{
    public Guid Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Price { get; set; }
}

public class DeleteKarmaProductRequest
{
    public Guid Id { get; set; }
}
