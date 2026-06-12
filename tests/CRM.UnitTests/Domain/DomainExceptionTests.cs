using CRM.Domain.Exceptions;

namespace CRM.UnitTests.Domain;

public class DomainExceptionTests
{
    [Fact]
    public void EntityNotFound_CarriesEntityNameAndKey()
    {
        var ex = new EntityNotFoundException("Customer", 42);

        ex.EntityName.Should().Be("Customer");
        ex.Key.Should().Be(42);
        ex.Message.Should().Contain("Customer").And.Contain("42");
        ex.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void DuplicateEntity_CarriesField_AndValue()
    {
        var ex = new DuplicateEntityException("User", "Email", "a@b.c");

        ex.EntityName.Should().Be("User");
        ex.Field.Should().Be("Email");
        ex.Value.Should().Be("a@b.c");
        ex.Message.Should().Contain("User").And.Contain("Email").And.Contain("a@b.c");
        ex.Should().BeAssignableTo<DomainException>();
    }
}
