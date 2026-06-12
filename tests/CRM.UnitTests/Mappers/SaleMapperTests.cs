using CRM.Application.Sales.Dtos;
using CRM.Application.Sales.Mapping;
using CRM.Domain.Entities;

namespace CRM.UnitTests.Mappers;

public class SaleMapperTests
{
    private static readonly DateOnly Today = new(2026, 6, 12);

    [Fact]
    public void ToDto_HandlesMissingNavigationProperties()
    {
        var s = new Sale
        {
            Id = 5,
            CustomerId = 1,
            UserId = 2,
            PipelineName = "X",
            Stage = "Proposal",
            Amount = 100,
            SaleDate = Today,
        };

        var dto = SaleMapper.ToDto(s);

        dto.Id.Should().Be(5);
        dto.CustomerName.Should().BeEmpty();
        dto.UserName.Should().BeEmpty();
        dto.Company.Should().BeNull();
    }

    [Fact]
    public void ToDto_UsesNavigationDataWhenLoaded()
    {
        var s = new Sale
        {
            Id = 5,
            CustomerId = 1,
            UserId = 2,
            PipelineName = "Enterprise",
            Stage = "ClosedWon",
            Amount = 9000,
            SaleDate = Today,
            Customer = new Customer { Id = 1, FirstName = "Jane", LastName = "Doe", Company = "Acme" },
            User = new User { Id = 2, Username = "owner", Email = "o@x.y", PasswordHash = "h", Role = "User" }
        };

        var dto = SaleMapper.ToDto(s);

        dto.CustomerName.Should().Be("Jane Doe");
        dto.UserName.Should().Be("owner");
        dto.Company.Should().Be("Acme");
    }

    [Fact]
    public void ToEntity_TrimsTextAndNullsBlankNotes()
    {
        var req = new CreateSaleRequest(
            CustomerId: 1,
            UserId: null,
            PipelineName: "  Enterprise  ",
            Stage: " Proposal ",
            Amount: 500m,
            SaleDate: Today,
            ExpectedCloseDate: Today.AddDays(5),
            Notes: "    ");

        var entity = SaleMapper.ToEntity(req);

        entity.PipelineName.Should().Be("Enterprise");
        entity.Stage.Should().Be("Proposal");
        entity.Notes.Should().BeNull();
    }

    [Fact]
    public void Apply_OverwritesScalars()
    {
        var existing = new Sale
        {
            CustomerId = 1,
            PipelineName = "Old",
            Stage = "Proposal",
            Amount = 100,
            SaleDate = Today.AddDays(-30)
        };

        var req = new UpdateSaleRequest(
            CustomerId: 9, "New", "Negotiation", 250m, Today, null, "more notes");

        SaleMapper.Apply(req, existing);

        existing.CustomerId.Should().Be(9);
        existing.PipelineName.Should().Be("New");
        existing.Stage.Should().Be("Negotiation");
        existing.Amount.Should().Be(250m);
        existing.SaleDate.Should().Be(Today);
        existing.ExpectedCloseDate.Should().BeNull();
        existing.Notes.Should().Be("more notes");
    }
}
