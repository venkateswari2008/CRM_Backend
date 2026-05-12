namespace CRM.Application.Sales.Dtos;

public sealed record UpdateSaleRequest(
    int CustomerId,
    string PipelineName,
    string Stage,
    decimal Amount,
    DateOnly SaleDate,
    DateOnly? ExpectedCloseDate,
    string? Notes);
