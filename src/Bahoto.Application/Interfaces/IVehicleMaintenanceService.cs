using Bahoto.Application.Common;
using Bahoto.Application.DTOs.VehicleMaintenance;

namespace Bahoto.Application.Interfaces;

public interface IVehicleMaintenanceService
{
    Task<ApiResponse<VehicleMaintenanceListData>> ListAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<VehicleMaintenanceDto>> CreateAsync(CreateVehicleMaintenanceRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<VehicleMaintenanceDto>> UpdateAsync(UpdateVehicleMaintenanceRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteAsync(DeleteVehicleMaintenanceRequest request, CancellationToken cancellationToken = default);
}
