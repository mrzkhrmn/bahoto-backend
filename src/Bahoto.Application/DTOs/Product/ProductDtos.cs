namespace Bahoto.Application.DTOs.Product;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateProductRequest
{
    public string Brand { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class UpdateProductRequest
{
    public Guid Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class GetProductRequest
{
    public Guid Id { get; set; }
}

public class DeleteProductRequest
{
    public Guid Id { get; set; }
}

public class ListProductRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
