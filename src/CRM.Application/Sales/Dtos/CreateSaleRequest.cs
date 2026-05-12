namespace CRM.Application.Sales.Dtos;

public sealed record CreateSaleRequest(
    int CustomerId,
    int? UserId,
    string PipelineName,
    string Stage,
    decimal Amount,
    DateOnly SaleDate,
    DateOnly? ExpectedCloseDate,
    string? Notes);
