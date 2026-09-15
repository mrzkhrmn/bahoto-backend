namespace Bahoto.Application.DTOs.OilChange;

public class OilChangeDto
{
    public Guid Id { get; set; }
    public string? Vehicle { get; set; }
    public string? Plate { get; set; }
    public string OilType { get; set; } = string.Empty;
    public int KmChanged { get; set; }
    public int NextChangeKm { get; set; }
    public string? OilFilter { get; set; }
    public string? AirFilter { get; set; }
    public string? FuelFilter { get; set; }
    public string? PolenFilter { get; set; }
    public string? Note { get; set; }
    public string? Employee { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateOilChangeRequest
{
    public string? Vehicle { get; set; }
    public string? Plate { get; set; }
    public string OilType { get; set; } = string.Empty;
    public int KmChanged { get; set; }
    public int? NextChangeKm { get; set; }
    public string? OilFilter { get; set; }
    public string? AirFilter { get; set; }
    public string? FuelFilter { get; set; }
    public string? PolenFilter { get; set; }
    public string? Note { get; set; }
    public string? Employee { get; set; }
}

public class UpdateOilChangeRequest
{
    public Guid Id { get; set; }
    public string? Vehicle { get; set; }
    public string? Plate { get; set; }
    public string OilType { get; set; } = string.Empty;
    public int KmChanged { get; set; }
    public int NextChangeKm { get; set; }
    public string? OilFilter { get; set; }
    public string? AirFilter { get; set; }
    public string? FuelFilter { get; set; }
    public string? PolenFilter { get; set; }
    public string? Note { get; set; }
    public string? Employee { get; set; }
}

public class GetOilChangeRequest
{
    public Guid Id { get; set; }
}

public class DeleteOilChangeRequest
{
    public Guid Id { get; set; }
}

public class ListOilChangeRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
