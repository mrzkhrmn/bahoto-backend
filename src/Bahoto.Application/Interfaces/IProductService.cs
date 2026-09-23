using Bahoto.Application.Common;
using Bahoto.Application.DTOs.OilChange;
using Bahoto.Application.DTOs.Product;

namespace Bahoto.Application.Interfaces;

public interface IProductService
{
    Task<ApiResponse<PagedResult<ProductDto>>> ListAsync(ListProductRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductDto>> GetAsync(GetProductRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductDto>> UpdateAsync(UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteAsync(DeleteProductRequest request, CancellationToken cancellationToken = default);
}
