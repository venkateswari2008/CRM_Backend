using System.IdentityModel.Tokens.Jwt;
using CRM.Application.Auth.Models;
using CRM.Domain.Entities;
using CRM.Infrastructure.Auth;
using CRM.UnitTests.TestSupport;
using Microsoft.Extensions.Options;

namespace CRM.UnitTests.Infrastructure;

public class JwtTokenGeneratorTests
{
    private static JwtSettings ValidSettings() => new()
    {
        Issuer = "crm.local",
        Audience = "crm.client",
        SigningKey = "kRxIgDw896glbSR5+NSIYOkoRRGmtCg6qoTves4kLEhBsU/Tj0F02TqAICXTm7un",
        AccessTokenMinutes = 60,
        ClockSkewSeconds = 30,
    };

    [Fact]
    public void Constructor_ThrowsWhenSigningKeyTooShort()
    {
        var s = ValidSettings();
        s.SigningKey = "tooshort";

        Action act = () => new JwtTokenGenerator(Options.Create(s), new FakeDateTimeProvider());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Generate_ProducesParseableJwtWithExpectedClaims()
    {
        var clock = new FakeDateTimeProvider();
        var gen = new JwtTokenGenerator(Options.Create(ValidSettings()), clock);

        var user = new User
        {
            Id = 42,
            Username = "alice",
            Email = "alice@crm.local",
            PasswordHash = "h",
            Role = "Admin",
        };
        var result = gen.Generate(user);

        result.TokenType.Should().Be("Bearer");
        result.ExpiresAt.Should().Be(clock.UtcNow.AddMinutes(60));

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.AccessToken);

        token.Issuer.Should().Be("crm.local");
        token.Audiences.Should().Contain("crm.client");
        token.Claims.Should().Contain(c => c.Type == "sub" && c.Value == "42");
        token.Claims.Should().Contain(c => c.Type == "unique_name" && c.Value == "alice");
        token.Claims.Should().Contain(c => c.Type == "email" && c.Value == "alice@crm.local");
        token.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Admin");
    }
}
