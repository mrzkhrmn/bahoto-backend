using Bahoto.Application.Common;
using Bahoto.Application.DTOs.Cari;
using Bahoto.Application.DTOs.OilChange;

namespace Bahoto.Application.Interfaces;

public interface ICariService
{
    Task<ApiResponse<PagedResult<CariDto>>> ListAsync(ListCariRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<CariDto>> GetAsync(GetCariRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<CariDto>> CreateAsync(CreateCariRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<CariDto>> UpdateAsync(UpdateCariRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteAsync(DeleteCariRequest request, CancellationToken cancellationToken = default);
}
