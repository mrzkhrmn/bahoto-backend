namespace Bahoto.Infrastructure.Options;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 1;
    public int RefreshTokenRememberDays { get; set; } = 30;
}
