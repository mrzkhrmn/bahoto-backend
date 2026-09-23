using Bahoto.Application.DTOs.Product;
using Bahoto.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bahoto.Api.Controllers;

[ApiController]
[Route("api/product")]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost("list")]
    public async Task<IActionResult> List([FromBody] ListProductRequest? request, CancellationToken cancellationToken)
    {
        var result = await _productService.ListAsync(request ?? new ListProductRequest(), cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("get")]
    public async Task<IActionResult> Get([FromBody] GetProductRequest request, CancellationToken cancellationToken)
    {
        var result = await _productService.GetAsync(request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await _productService.CreateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await _productService.UpdateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteProductRequest request, CancellationToken cancellationToken)
    {
        var result = await _productService.DeleteAsync(request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
