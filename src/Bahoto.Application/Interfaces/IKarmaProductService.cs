using Bahoto.Application.Common;
using Bahoto.Application.DTOs.KarmaProduct;

namespace Bahoto.Application.Interfaces;

public interface IKarmaProductService
{
    Task<ApiResponse<KarmaProductListData>> ListAsync(
        ListKarmaProductRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<KarmaProductDto>> CreateAsync(
        CreateKarmaProductRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<KarmaProductDto>> UpdateAsync(
        UpdateKarmaProductRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse> DeleteAsync(
        DeleteKarmaProductRequest request,
        CancellationToken cancellationToken = default);
}
