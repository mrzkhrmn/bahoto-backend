namespace Bahoto.Application.DTOs.Cari;

public class CariDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductBrand { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string QuantityUnit { get; set; } = "Adet";
    public decimal IncomingAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CariProductGroupDto
{
    public Guid ProductId { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string QuantityLabel { get; set; } = string.Empty;
    public decimal IncomingAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public List<CariDto> Entries { get; set; } = [];
}

public class CreateCariRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public string QuantityUnit { get; set; } = "Adet";
    public decimal IncomingAmount { get; set; }
    public decimal PaidAmount { get; set; }
}

public class CreateCariWithProductRequest
{
    public string Brand { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? Quantity { get; set; }
    public string? QuantityUnit { get; set; }
    public decimal? IncomingAmount { get; set; }
    public decimal? PaidAmount { get; set; }
}

public class UpdateCariRequest
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public string QuantityUnit { get; set; } = "Adet";
    public decimal IncomingAmount { get; set; }
    public decimal PaidAmount { get; set; }
}

public class GetCariRequest
{
    public Guid Id { get; set; }
}

public class DeleteCariRequest
{
    public Guid Id { get; set; }
}

public class ListCariRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
