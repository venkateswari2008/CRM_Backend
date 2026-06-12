using CRM.Application.Customers.Dtos;
using CRM.Application.Customers.Validators;

namespace CRM.UnitTests.Validators;

public class CustomerValidatorTests
{
    private readonly CreateCustomerRequestValidator _createSut = new();
    private readonly UpdateCustomerRequestValidator _updateSut = new();

    private static CreateCustomerRequest ValidCreate() =>
        new("John", "Doe", "john@example.com", "(123) 456-7890", null, null, null, null, null, null, null);

    private static UpdateCustomerRequest ValidUpdate() =>
        new("John", "Doe", "john@example.com", "(123) 456-7890", null, null, null, null, null, null, null);

    [Fact]
    public void Create_Passes_OnGoodPayload()
    {
        _createSut.Validate(ValidCreate()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Update_Passes_OnGoodPayload()
    {
        _updateSut.Validate(ValidUpdate()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Doe", "john@example.com")]
    [InlineData("John", "", "john@example.com")]
    [InlineData("John", "Doe", "")]
    [InlineData("John", "Doe", "not-an-email")]
    public void Fails_WhenRequiredFieldsMissingOrInvalid(string first, string last, string email)
    {
        _createSut.Validate(ValidCreate() with { FirstName = first, LastName = last, Email = email })
            .IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("abc")]            // letters not allowed in phone
    [InlineData("123-abc-xyz")]
    public void Create_Fails_OnInvalidPhone(string phone)
    {
        _createSut.Validate(ValidCreate() with { Phone = phone }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Create_Accepts_NullPhone()
    {
        _createSut.Validate(ValidCreate() with { Phone = null }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Create_Fails_OnOverlongFirstName()
    {
        _createSut.Validate(ValidCreate() with { FirstName = new string('a', 51) })
            .IsValid.Should().BeFalse();
    }
}
