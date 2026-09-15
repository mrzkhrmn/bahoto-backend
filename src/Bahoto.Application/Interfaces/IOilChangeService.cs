using Bahoto.Application.Common;
using Bahoto.Application.DTOs.OilChange;

namespace Bahoto.Application.Interfaces;

public interface IOilChangeService
{
    Task<ApiResponse<PagedResult<OilChangeDto>>> ListAsync(ListOilChangeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<OilChangeDto>> GetAsync(GetOilChangeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<OilChangeDto>> CreateAsync(CreateOilChangeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<OilChangeDto>> UpdateAsync(UpdateOilChangeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteAsync(DeleteOilChangeRequest request, CancellationToken cancellationToken = default);
}
