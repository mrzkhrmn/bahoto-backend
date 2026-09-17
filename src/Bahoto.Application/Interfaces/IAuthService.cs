using Bahoto.Application.Common;
using Bahoto.Application.DTOs.Auth;

namespace Bahoto.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
}
