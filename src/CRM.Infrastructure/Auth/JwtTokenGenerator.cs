using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CRM.Application.Abstractions;
using CRM.Application.Auth.Models;
using CRM.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CRM.Infrastructure.Auth;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;
    private readonly IDateTimeProvider _clock;

    public JwtTokenGenerator(IOptions<JwtSettings> settings, IDateTimeProvider clock)
    {
        _settings = settings.Value;
        _clock = clock;

        if (string.IsNullOrWhiteSpace(_settings.SigningKey) ||
            Encoding.UTF8.GetByteCount(_settings.SigningKey) < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must be configured and be at least 32 bytes (256 bits).");
        }
    }

    public JwtTokenResult Generate(User user)
    {
        var expiresAt = _clock.UtcNow.AddMinutes(_settings.AccessTokenMinutes);

        // Use short claim names that line up with TokenValidationParameters in Program.cs
        // (NameClaimType = "unique_name", RoleClaimType = "role"). Avoid ClaimTypes.* long URIs
        // — JwtSecurityTokenHandler does not rewrite them on outbound, so they would survive into
        // the JWT payload as long URIs and miss the validator's role/name lookup.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("role", user.Role),
        };

        var keyBytes = Encoding.UTF8.GetBytes(_settings.SigningKey);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: _clock.UtcNow.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return new JwtTokenResult(token, expiresAt, "Bearer");
    }
}
