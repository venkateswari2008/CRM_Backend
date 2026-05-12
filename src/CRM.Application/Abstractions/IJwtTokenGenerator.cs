using CRM.Domain.Entities;

namespace CRM.Application.Abstractions;

public interface IJwtTokenGenerator
{
    JwtTokenResult Generate(User user);
}

public sealed record JwtTokenResult(string AccessToken, DateTimeOffset ExpiresAt, string TokenType);
