using Bahoto.Application.DTOs.Cari;
using Bahoto.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bahoto.Api.Controllers;

[ApiController]
[Route("api/cari")]
[Authorize]
public class CariController : ControllerBase
{
    private readonly ICariService _cariService;

    public CariController(ICariService cariService)
    {
        _cariService = cariService;
    }

    [HttpPost("list")]
    public async Task<IActionResult> List([FromBody] ListCariRequest? request, CancellationToken cancellationToken)
    {
        var result = await _cariService.ListAsync(request ?? new ListCariRequest(), cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("get")]
    public async Task<IActionResult> Get([FromBody] GetCariRequest request, CancellationToken cancellationToken)
    {
        var result = await _cariService.GetAsync(request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateCariRequest request, CancellationToken cancellationToken)
    {
        var result = await _cariService.CreateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody] UpdateCariRequest request, CancellationToken cancellationToken)
    {
        var result = await _cariService.UpdateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteCariRequest request, CancellationToken cancellationToken)
    {
        var result = await _cariService.DeleteAsync(request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
