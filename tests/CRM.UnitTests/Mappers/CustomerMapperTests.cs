using CRM.Application.Customers.Dtos;
using CRM.Application.Customers.Mapping;
using CRM.Domain.Entities;

namespace CRM.UnitTests.Mappers;

public class CustomerMapperTests
{
    [Fact]
    public void ToDto_ProjectsAllFields_AndComputesFullName()
    {
        var entity = new Customer
        {
            Id = 7,
            FirstName = "Jane",
            LastName = "Roe",
            Email = "jane@example.com",
            Phone = "(555) 999-1234",
            City = "Boston",
            Country = "USA",
            Company = "Globex",
            CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        };

        var dto = CustomerMapper.ToDto(entity);

        dto.Id.Should().Be(7);
        dto.FullName.Should().Be("Jane Roe");
        dto.City.Should().Be("Boston");
        dto.Company.Should().Be("Globex");
        dto.Email.Should().Be("jane@example.com");
    }

    [Fact]
    public void ToEntity_TrimsAndLowercasesEmail_AndNullifiesBlankOptionalFields()
    {
        var req = new CreateCustomerRequest(
            "  Jane  ", " Roe ", "  Jane@Example.com  ",
            Phone: "  ",
            AddressLine: " 1 St ",
            City: "",
            State: "    ",
            ZipCode: "12345",
            Country: "USA",
            Company: null,
            Notes: "  note  ");

        var entity = CustomerMapper.ToEntity(req);

        entity.FirstName.Should().Be("Jane");
        entity.LastName.Should().Be("Roe");
        entity.Email.Should().Be("jane@example.com");
        entity.Phone.Should().BeNull();
        entity.AddressLine.Should().Be("1 St");
        entity.City.Should().BeNull();
        entity.State.Should().BeNull();
        entity.ZipCode.Should().Be("12345");
        entity.Company.Should().BeNull();
        entity.Notes.Should().Be("note");
    }

    [Fact]
    public void Apply_OverwritesEntityFields()
    {
        var entity = new Customer
        {
            FirstName = "Old",
            LastName = "Name",
            Email = "old@example.com",
            City = "Old City",
        };

        var req = new UpdateCustomerRequest(
            "New", "Name", "NEW@example.com", null, null, "New City", null, null, null, null, null);

        CustomerMapper.Apply(req, entity);

        entity.FirstName.Should().Be("New");
        entity.Email.Should().Be("new@example.com");
        entity.City.Should().Be("New City");
        entity.AddressLine.Should().BeNull();
    }

    [Fact]
    public void Projection_IsAnExpressionUsableByEf()
    {
        var entity = new Customer
        {
            Id = 1, FirstName = "A", LastName = "B", Email = "a@b.c"
        };
        var dto = CustomerMapper.Projection.Compile()(entity);
        dto.FullName.Should().Be("A B");
        dto.Email.Should().Be("a@b.c");
    }
}
