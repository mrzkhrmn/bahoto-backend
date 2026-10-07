using Bahoto.Application.Common;
using Bahoto.Application.DTOs.WashPrice;

namespace Bahoto.Application.Interfaces;

public interface IWashPriceService
{
    Task<ApiResponse<WashPriceListData>> ListAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<WashPriceDto>> CreateAsync(CreateWashPriceRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<WashPriceDto>> UpdateAsync(UpdateWashPriceRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteAsync(DeleteWashPriceRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse> ReorderAsync(ReorderWashPriceRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ApplyServicePriceIncreaseResultDto>> ApplyPriceIncreaseAsync(ApplyWashPriceIncreaseRequest request, CancellationToken cancellationToken = default);
}
