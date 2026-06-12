using CRM.Domain.Enums;

namespace CRM.UnitTests.Domain;

public class SaleStagesTests
{
    [Fact]
    public void All_ContainsTheFiveStages()
    {
        SaleStages.All.Should().BeEquivalentTo(new[]
        {
            "Qualification", "Proposal", "Negotiation", "ClosedWon", "ClosedLost"
        });
    }

    [Fact]
    public void ClosedStages_ContainsOnlyClosedWonAndClosedLost()
    {
        SaleStages.ClosedStages.Should().BeEquivalentTo(new[] { "ClosedWon", "ClosedLost" });
    }

    [Theory]
    [InlineData("Qualification", true)]
    [InlineData("Proposal", true)]
    [InlineData("Negotiation", true)]
    [InlineData("ClosedWon", true)]
    [InlineData("ClosedLost", true)]
    [InlineData("closedwon", false)]
    [InlineData("Random", false)]
    [InlineData("", false)]
    public void IsValid_DiscriminatesKnownStages(string? stage, bool expected)
    {
        SaleStages.IsValid(stage).Should().Be(expected);
    }

    [Theory]
    [InlineData("ClosedWon", true)]
    [InlineData("ClosedLost", true)]
    [InlineData("Proposal", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsClosed_OnlyTrueForTerminalStages(string? stage, bool expected)
    {
        SaleStages.IsClosed(stage).Should().Be(expected);
    }
}
