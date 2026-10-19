namespace Bahoto.Application.DTOs.VehicleMaintenance;

public class VehicleMaintenanceDto
{
    public Guid Id { get; set; }
    public string BrandModel { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? EngineOil { get; set; }
    public string? AirFilter { get; set; }
    public string? OilFilter { get; set; }
    public string? FuelFilter { get; set; }
    public string? PolenFilter { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class VehicleMaintenanceListData
{
    public List<VehicleMaintenanceDto> Items { get; set; } = [];
}

public class CreateVehicleMaintenanceRequest
{
    public string BrandModel { get; set; } = string.Empty;
    public string? EngineOil { get; set; }
    public string? AirFilter { get; set; }
    public string? OilFilter { get; set; }
    public string? FuelFilter { get; set; }
    public string? PolenFilter { get; set; }
}

public class UpdateVehicleMaintenanceRequest : CreateVehicleMaintenanceRequest
{
    public Guid Id { get; set; }
}

public class DeleteVehicleMaintenanceRequest
{
    public Guid Id { get; set; }
}
