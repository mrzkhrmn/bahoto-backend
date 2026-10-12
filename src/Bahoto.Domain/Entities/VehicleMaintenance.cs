using Bahoto.Domain.Common;

namespace Bahoto.Domain.Entities;

public class VehicleMaintenance : BaseEntity
{
    public string BrandModel { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? EngineOil { get; set; }
    public string? AirFilter { get; set; }
    public string? OilFilter { get; set; }
    public string? PolenFilter { get; set; }
}
