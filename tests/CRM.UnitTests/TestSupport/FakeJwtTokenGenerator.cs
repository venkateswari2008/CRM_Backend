using CRM.Application.Abstractions;
using CRM.Domain.Entities;

namespace CRM.UnitTests.TestSupport;

internal sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
{
    public int CallCount { get; private set; }

    public JwtTokenResult Generate(User user)
    {
        CallCount++;
        return new JwtTokenResult(
            AccessToken: $"fake-token-{user.Id}",
            ExpiresAt: new DateTimeOffset(2026, 6, 12, 13, 0, 0, TimeSpan.Zero),
            TokenType: "Bearer");
    }
}
