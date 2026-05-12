namespace CRM.Application.Customers.Dtos;

public sealed record CreateCustomerRequest(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? AddressLine,
    string? City,
    string? State,
    string? ZipCode,
    string? Country,
    string? Company,
    string? Notes);
