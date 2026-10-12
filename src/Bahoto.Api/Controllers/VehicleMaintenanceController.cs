using Bahoto.Application.DTOs.VehicleMaintenance;
using Bahoto.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bahoto.Api.Controllers;

[ApiController]
[Route("api/vehicle-maintenance")]
[Authorize]
public class VehicleMaintenanceController : ControllerBase
{
    private readonly IVehicleMaintenanceService _service;

    public VehicleMaintenanceController(IVehicleMaintenanceService service)
    {
        _service = service;
    }

    [HttpPost("list")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _service.ListAsync(cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(
        [FromBody] CreateVehicleMaintenanceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update(
        [FromBody] UpdateVehicleMaintenanceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete(
        [FromBody] DeleteVehicleMaintenanceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
