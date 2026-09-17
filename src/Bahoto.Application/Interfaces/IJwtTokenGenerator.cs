using Bahoto.Domain.Entities;

namespace Bahoto.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
    int AccessTokenExpirationMinutes { get; }
    int GetRefreshTokenExpirationDays(bool rememberMe);
}
