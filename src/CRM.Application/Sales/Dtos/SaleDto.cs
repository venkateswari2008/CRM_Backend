namespace CRM.Application.Sales.Dtos;

public sealed record SaleDto(
    int Id,
    int CustomerId,
    string CustomerName,
    string? Company,
    int UserId,
    string UserName,
    string PipelineName,
    string Stage,
    decimal Amount,
    DateOnly SaleDate,
    DateOnly? ExpectedCloseDate,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
