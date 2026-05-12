namespace CRM.Application.Customers.Dtos;

public sealed record CustomerDto(
    int Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? Phone,
    string? AddressLine,
    string? City,
    string? State,
    string? ZipCode,
    string? Country,
    string? Company,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
