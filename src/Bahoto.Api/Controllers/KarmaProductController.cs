using Bahoto.Application.DTOs.KarmaProduct;
using Bahoto.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bahoto.Api.Controllers;

[ApiController]
[Route("api/karma-product")]
[Authorize]
public class KarmaProductController : ControllerBase
{
    private readonly IKarmaProductService _service;

    public KarmaProductController(IKarmaProductService service)
    {
        _service = service;
    }

    [HttpPost("list")]
    public async Task<IActionResult> List(
        [FromBody] ListKarmaProductRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _service.ListAsync(
            request ?? new ListKarmaProductRequest(),
            cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(
        [FromBody] CreateKarmaProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update(
        [FromBody] UpdateKarmaProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete(
        [FromBody] DeleteKarmaProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
