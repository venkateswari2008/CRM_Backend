using System.Linq.Expressions;
using CRM.Application.Customers.Dtos;
using CRM.Domain.Entities;

namespace CRM.Application.Customers.Mapping;

/// <summary>
/// Hand-written, allocation-friendly mappers. Using an <see cref="Expression"/>-based
/// projection lets EF Core translate to SQL and avoid materialising entire entities.
/// </summary>
public static class CustomerMapper
{
    public static Expression<Func<Customer, CustomerDto>> Projection { get; } = c => new CustomerDto(
        c.Id,
        c.FirstName,
        c.LastName,
        (c.FirstName + " " + c.LastName).Trim(),
        c.Email,
        c.Phone,
        c.AddressLine,
        c.City,
        c.State,
        c.ZipCode,
        c.Country,
        c.Company,
        c.Notes,
        c.CreatedAt,
        c.UpdatedAt);

    public static CustomerDto ToDto(Customer c) => new(
        c.Id,
        c.FirstName,
        c.LastName,
        c.FullName,
        c.Email,
        c.Phone,
        c.AddressLine,
        c.City,
        c.State,
        c.ZipCode,
        c.Country,
        c.Company,
        c.Notes,
        c.CreatedAt,
        c.UpdatedAt);

    public static Customer ToEntity(CreateCustomerRequest r) => new()
    {
        FirstName = r.FirstName.Trim(),
        LastName = r.LastName.Trim(),
        Email = r.Email.Trim().ToLowerInvariant(),
        Phone = Trim(r.Phone),
        AddressLine = Trim(r.AddressLine),
        City = Trim(r.City),
        State = Trim(r.State),
        ZipCode = Trim(r.ZipCode),
        Country = Trim(r.Country),
        Company = Trim(r.Company),
        Notes = Trim(r.Notes),
    };

    public static void Apply(UpdateCustomerRequest r, Customer c)
    {
        c.FirstName = r.FirstName.Trim();
        c.LastName = r.LastName.Trim();
        c.Email = r.Email.Trim().ToLowerInvariant();
        c.Phone = Trim(r.Phone);
        c.AddressLine = Trim(r.AddressLine);
        c.City = Trim(r.City);
        c.State = Trim(r.State);
        c.ZipCode = Trim(r.ZipCode);
        c.Country = Trim(r.Country);
        c.Company = Trim(r.Company);
        c.Notes = Trim(r.Notes);
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
