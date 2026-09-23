namespace Bahoto.Application.DTOs.Cari;

public class CariDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductBrand { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal IncomingAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCariRequest
{
    public Guid ProductId { get; set; }
    public decimal IncomingAmount { get; set; }
    public decimal PaidAmount { get; set; }
}

public class UpdateCariRequest
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
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
}
