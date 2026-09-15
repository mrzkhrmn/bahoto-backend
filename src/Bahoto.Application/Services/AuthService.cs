using Bahoto.Application.Common;
using Bahoto.Application.DTOs.Auth;
using Bahoto.Application.Interfaces;
using Bahoto.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bahoto.Application.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(
        IApplicationDbContext db,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.FullName))
        {
            return ApiResponse<AuthResponse>.Fail("Email, şifre ve ad soyad zorunludur.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await _db.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (exists)
        {
            return ApiResponse<AuthResponse>.Fail("Bu email adresi zaten kayıtlı.");
        }

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim()
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<AuthResponse>.Ok(ToAuthResponse(user), "Kayıt başarılı.");
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiResponse<AuthResponse>.Fail("Email ve şifre zorunludur.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null)
        {
            return ApiResponse<AuthResponse>.Fail("Email veya şifre hatalı.");
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return ApiResponse<AuthResponse>.Fail("Email veya şifre hatalı.");
        }

        return ApiResponse<AuthResponse>.Ok(ToAuthResponse(user), "Giriş başarılı.");
    }

    private AuthResponse ToAuthResponse(User user) => new()
    {
        UserId = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        Token = _jwtTokenGenerator.GenerateToken(user)
    };
}
