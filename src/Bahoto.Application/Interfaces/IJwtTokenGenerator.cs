using Bahoto.Domain.Entities;

namespace Bahoto.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
