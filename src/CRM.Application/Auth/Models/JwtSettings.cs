namespace CRM.Application.Auth.Models;

/// <summary>Strongly-typed JWT configuration bound from configuration section <c>Jwt</c>.</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>Symmetric signing key. MUST be at least 32 bytes / 256 bits.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 60;

    public int ClockSkewSeconds { get; set; } = 30;
}
