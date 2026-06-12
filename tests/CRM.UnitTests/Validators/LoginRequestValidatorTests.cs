using CRM.Application.Auth.Dtos;
using CRM.Application.Auth.Validators;

namespace CRM.UnitTests.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _sut = new();

    [Fact]
    public void Passes_WhenBothFieldsPresent()
    {
        var r = new LoginRequest("admin", "ChangeMe!123");
        _sut.Validate(r).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "pwd")]
    [InlineData(" ", "pwd")]
    [InlineData("admin", "")]
    [InlineData("admin", " ")]
    public void Fails_WhenEitherFieldBlank(string user, string pwd)
    {
        _sut.Validate(new LoginRequest(user, pwd)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Fails_WhenUsernameExceedsHundredChars()
    {
        var longUser = new string('a', 101);
        _sut.Validate(new LoginRequest(longUser, "pwd")).IsValid.Should().BeFalse();
    }
}
