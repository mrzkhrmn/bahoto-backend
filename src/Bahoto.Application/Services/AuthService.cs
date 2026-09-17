using System.Security.Cryptography;
using System.Text;
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

        var response = await IssueTokensAsync(user, rememberMe: false, cancellationToken);
        return ApiResponse<AuthResponse>.Ok(response, "Kayıt başarılı.");
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

        var response = await IssueTokensAsync(user, request.RememberMe, cancellationToken);
        return ApiResponse<AuthResponse>.Ok(response, "Giriş başarılı.");
    }

    public async Task<ApiResponse<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return ApiResponse<AuthResponse>.Fail("Refresh token zorunludur.");
        }

        var tokenHash = HashToken(request.RefreshToken);
        var stored = await _db.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (stored is null || !stored.IsActive || stored.User is null)
        {
            return ApiResponse<AuthResponse>.Fail("Refresh token geçersiz veya süresi dolmuş.");
        }

        var response = await RotateRefreshTokenAsync(stored, stored.RememberMe, cancellationToken);
        return ApiResponse<AuthResponse>.Ok(response, "Token yenilendi.");
    }

    public async Task<ApiResponse> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return ApiResponse.Ok("Çıkış başarılı.");
        }

        var tokenHash = HashToken(request.RefreshToken);
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (stored is not null && stored.RevokedAt is null)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse.Ok("Çıkış başarılı.");
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, bool rememberMe, CancellationToken cancellationToken)
    {
        var rawRefresh = GenerateRefreshToken();
        var refreshHash = HashToken(rawRefresh);
        var days = _jwtTokenGenerator.GetRefreshTokenExpirationDays(rememberMe);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = DateTime.UtcNow.AddDays(days),
            RememberMe = rememberMe
        });
        await _db.SaveChangesAsync(cancellationToken);

        return ToAuthResponse(user, rawRefresh);
    }

    private async Task<AuthResponse> RotateRefreshTokenAsync(
        RefreshToken current,
        bool rememberMe,
        CancellationToken cancellationToken)
    {
        var rawRefresh = GenerateRefreshToken();
        var refreshHash = HashToken(rawRefresh);
        var days = _jwtTokenGenerator.GetRefreshTokenExpirationDays(rememberMe);

        current.RevokedAt = DateTime.UtcNow;
        current.ReplacedByTokenHash = refreshHash;

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = current.UserId,
            TokenHash = refreshHash,
            ExpiresAt = DateTime.UtcNow.AddDays(days),
            RememberMe = rememberMe
        });
        await _db.SaveChangesAsync(cancellationToken);

        return ToAuthResponse(current.User, rawRefresh);
    }

    private AuthResponse ToAuthResponse(User user, string refreshToken) => new()
    {
        UserId = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        Token = _jwtTokenGenerator.GenerateToken(user),
        RefreshToken = refreshToken,
        ExpiresIn = _jwtTokenGenerator.AccessTokenExpirationMinutes * 60
    };

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
