using CRM.Application.Auth.Dtos;
using CRM.Application.Auth.Validators;

namespace CRM.UnitTests.Validators;

public class SignupRequestValidatorTests
{
    private readonly SignupRequestValidator _sut = new();

    private static SignupRequest Valid() =>
        new("alice", "alice@example.com", "Password1!", "User");

    [Fact]
    public void Passes_WithGoodPayload()
    {
        _sut.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Passes_WithoutExplicitRole()
    {
        _sut.Validate(Valid() with { Role = null }).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("with space")]
    [InlineData("bad!chars")]
    public void Fails_OnBadUsername(string username)
    {
        _sut.Validate(Valid() with { Username = username }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Fails_OnInvalidEmail()
    {
        _sut.Validate(Valid() with { Email = "not-an-email" }).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("short1!")]        // < 8 chars
    [InlineData("nocapital1!")]     // missing uppercase
    [InlineData("NOLOWERS1!")]      // missing lowercase
    [InlineData("NoDigitsHere!")]   // missing digit
    [InlineData("NoSymbols123")]    // missing symbol
    public void Fails_OnWeakPassword(string password)
    {
        _sut.Validate(Valid() with { Password = password }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Fails_OnInvalidRole()
    {
        _sut.Validate(Valid() with { Role = "SuperAdmin" }).IsValid.Should().BeFalse();
    }
}
