using CRM.Application.Sales.Dtos;
using CRM.Application.Sales.Validators;

namespace CRM.UnitTests.Validators;

public class SaleValidatorTests
{
    private readonly CreateSaleRequestValidator _createSut = new();
    private readonly UpdateSaleRequestValidator _updateSut = new();
    private static readonly DateOnly Today = new(2026, 6, 12);

    private static CreateSaleRequest ValidCreate() =>
        new(CustomerId: 1, UserId: 2, "Enterprise", "Proposal", 1000m, Today, Today.AddDays(10), "notes");

    private static UpdateSaleRequest ValidUpdate() =>
        new(CustomerId: 1, "Enterprise", "Proposal", 1000m, Today, Today.AddDays(10), "notes");

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
    [InlineData(0)]
    [InlineData(-3)]
    public void Create_Fails_OnNonPositiveCustomerId(int id)
    {
        _createSut.Validate(ValidCreate() with { CustomerId = id }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Create_Fails_OnUnknownStage()
    {
        _createSut.Validate(ValidCreate() with { Stage = "Won" }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Create_Fails_OnNegativeAmount()
    {
        _createSut.Validate(ValidCreate() with { Amount = -1m }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Create_Fails_OnEnormousAmount()
    {
        _createSut.Validate(ValidCreate() with { Amount = 1_000_000_000m }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Create_Fails_WhenExpectedCloseIsBeforeSaleDate()
    {
        _createSut.Validate(ValidCreate() with { ExpectedCloseDate = Today.AddDays(-1) })
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Create_Allows_NullExpectedCloseDate()
    {
        _createSut.Validate(ValidCreate() with { ExpectedCloseDate = null }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Update_Fails_OnUnknownStage()
    {
        _updateSut.Validate(ValidUpdate() with { Stage = "" }).IsValid.Should().BeFalse();
    }
}
