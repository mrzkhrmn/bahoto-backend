using Bahoto.Application.DTOs.DryLubePrice;
using Bahoto.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bahoto.Api.Controllers;

[ApiController]
[Route("api/dry-lube-price")]
[Authorize]
public class DryLubePriceController : ControllerBase
{
    private readonly IDryLubePriceService _service;

    public DryLubePriceController(IDryLubePriceService service)
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
    public async Task<IActionResult> Create([FromBody] CreateDryLubePriceRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody] UpdateDryLubePriceRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteDryLubePriceRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReorderDryLubePriceRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.ReorderAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("apply-price-increase")]
    public async Task<IActionResult> ApplyPriceIncrease([FromBody] ApplyDryLubePriceIncreaseRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.ApplyPriceIncreaseAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
