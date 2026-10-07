using Bahoto.Application.Common;
using Bahoto.Application.DTOs.DryLubePrice;

namespace Bahoto.Application.Interfaces;

public interface IDryLubePriceService
{
    Task<ApiResponse<DryLubePriceListData>> ListAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<DryLubePriceDto>> CreateAsync(CreateDryLubePriceRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<DryLubePriceDto>> UpdateAsync(UpdateDryLubePriceRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteAsync(DeleteDryLubePriceRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse> ReorderAsync(ReorderDryLubePriceRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ApplyDryLubePriceIncreaseResultDto>> ApplyPriceIncreaseAsync(ApplyDryLubePriceIncreaseRequest request, CancellationToken cancellationToken = default);
}
