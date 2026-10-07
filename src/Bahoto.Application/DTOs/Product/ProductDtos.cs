namespace Bahoto.Application.DTOs.Product;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public DateTime? LastPriceDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ProductBrandGroupDto
{
    public string Brand { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public List<ProductDto> Products { get; set; } = [];
}

public class CreateProductRequest
{
    public string Brand { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Price { get; set; }
}

public class UpdateProductRequest
{
    public Guid Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Price { get; set; }
}

public class GetProductRequest
{
    public Guid Id { get; set; }
}

public class DeleteProductRequest
{
    public Guid Id { get; set; }
}

public class DeleteBrandRequest
{
    public string Brand { get; set; } = string.Empty;
}

public class ReorderBrandsRequest
{
    public List<string> Brands { get; set; } = [];
}

public class ListProductRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
}

public class ApplyPriceIncreaseRequest
{
    /// <summary>all | brand | product</summary>
    public string Scope { get; set; } = "all";
    public string? Brand { get; set; }
    public Guid? ProductId { get; set; }
    /// <summary>Yüzde zam; örn. 10 = %10 artış. Negatif değer indirim uygular.</summary>
    public decimal Percent { get; set; }
}

public class ApplyPriceIncreaseResultDto
{
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
}
