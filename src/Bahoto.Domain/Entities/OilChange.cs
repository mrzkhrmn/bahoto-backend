using Bahoto.Domain.Common;

namespace Bahoto.Domain.Entities;

public class OilChange : BaseEntity
{
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
