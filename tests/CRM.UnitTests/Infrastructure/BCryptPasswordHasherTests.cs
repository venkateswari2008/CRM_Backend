using CRM.Infrastructure.Auth;

namespace CRM.UnitTests.Infrastructure;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _sut = new();

    [Fact]
    public void Hash_ReturnsBcryptFormattedString()
    {
        var hash = _sut.Hash("MyP@ssw0rd!");

        hash.Should().StartWith("$2");
        hash.Length.Should().BeGreaterThan(50);
    }

    [Fact]
    public void Hash_ProducesDifferentSaltEachCall()
    {
        var a = _sut.Hash("same");
        var b = _sut.Hash("same");
        a.Should().NotBe(b);
    }

    [Fact]
    public void Verify_TrueForMatchingPassword()
    {
        var hash = _sut.Hash("MyP@ssw0rd!");
        _sut.Verify("MyP@ssw0rd!", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_FalseForWrongPassword()
    {
        var hash = _sut.Hash("MyP@ssw0rd!");
        _sut.Verify("Different", hash).Should().BeFalse();
    }

    [Theory]
    [InlineData("", "$2a$11$abcdefghijklmnopqrstuv")]
    [InlineData("pwd", "")]
    [InlineData("pwd", "not-a-bcrypt-hash")]
    public void Verify_ReturnsFalse_OnEmptyOrMalformedInputs(string password, string hash)
    {
        _sut.Verify(password, hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_Throws_OnEmptyPassword()
    {
        Action act = () => _sut.Hash("");
        act.Should().Throw<ArgumentException>();
    }
}
