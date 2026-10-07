using Bahoto.Application.DTOs.WashPrice;
using Bahoto.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bahoto.Api.Controllers;

[ApiController]
[Route("api/wash-price")]
[Authorize]
public class WashPriceController : ControllerBase
{
    private readonly IWashPriceService _service;

    public WashPriceController(IWashPriceService service)
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
    public async Task<IActionResult> Create([FromBody] CreateWashPriceRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody] UpdateWashPriceRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteWashPriceRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReorderWashPriceRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.ReorderAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("apply-price-increase")]
    public async Task<IActionResult> ApplyPriceIncrease([FromBody] ApplyWashPriceIncreaseRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.ApplyPriceIncreaseAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
