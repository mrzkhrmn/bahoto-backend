using Bahoto.Application.DTOs.OilChange;
using Bahoto.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bahoto.Api.Controllers;

[ApiController]
[Route("api/oilchange")]
[Authorize]
public class OilChangeController : ControllerBase
{
    private readonly IOilChangeService _oilChangeService;

    public OilChangeController(IOilChangeService oilChangeService)
    {
        _oilChangeService = oilChangeService;
    }

    [HttpPost("list")]
    public async Task<IActionResult> List([FromBody] ListOilChangeRequest? request, CancellationToken cancellationToken)
    {
        var result = await _oilChangeService.ListAsync(request ?? new ListOilChangeRequest(), cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("get")]
    public async Task<IActionResult> Get([FromBody] GetOilChangeRequest request, CancellationToken cancellationToken)
    {
        var result = await _oilChangeService.GetAsync(request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateOilChangeRequest request, CancellationToken cancellationToken)
    {
        var result = await _oilChangeService.CreateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody] UpdateOilChangeRequest request, CancellationToken cancellationToken)
    {
        var result = await _oilChangeService.UpdateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteOilChangeRequest request, CancellationToken cancellationToken)
    {
        var result = await _oilChangeService.DeleteAsync(request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
