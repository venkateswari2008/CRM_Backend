using CRM.Domain.Entities;

namespace CRM.UnitTests.Domain;

public class EntityTests
{
    [Fact]
    public void Customer_FullName_ConcatenatesFirstAndLast()
    {
        var c = new Customer { FirstName = "Jane", LastName = "Doe" };
        c.FullName.Should().Be("Jane Doe");
    }

    [Fact]
    public void Customer_FullName_FallsBackToFirstNameWhenLastIsBlank()
    {
        var c = new Customer { FirstName = "Cher", LastName = "" };
        c.FullName.Should().Be("Cher");
    }

    [Fact]
    public void Customer_FullName_TrimsLeadingAndTrailingWhitespace()
    {
        var c = new Customer { FirstName = "  Ada", LastName = "Lovelace  " };
        c.FullName.Should().Be("Ada Lovelace");
    }

    [Fact]
    public void User_Defaults_AreSafe()
    {
        var u = new User();
        u.IsActive.Should().BeTrue();
        u.FailedLoginAttempts.Should().Be(0);
        u.OwnedSales.Should().NotBeNull().And.BeEmpty();
        u.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Sale_Defaults_AreSafe()
    {
        var s = new Sale();
        s.Amount.Should().Be(0);
        s.IsDeleted.Should().BeFalse();
        s.PipelineName.Should().BeEmpty();
        s.Stage.Should().BeEmpty();
    }
}
