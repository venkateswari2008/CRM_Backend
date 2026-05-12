using CRM.Application.Sales.Dtos;
using CRM.Domain.Entities;

namespace CRM.Application.Sales.Mapping;

public static class SaleMapper
{
    public static SaleDto ToDto(Sale s) => new(
        s.Id,
        s.CustomerId,
        s.Customer is null ? string.Empty : s.Customer.FullName,
        s.Customer?.Company,
        s.UserId,
        s.User is null ? string.Empty : s.User.Username,
        s.PipelineName,
        s.Stage,
        s.Amount,
        s.SaleDate,
        s.ExpectedCloseDate,
        s.Notes,
        s.CreatedAt,
        s.UpdatedAt);

    public static Sale ToEntity(CreateSaleRequest r) => new()
    {
        CustomerId = r.CustomerId,
        PipelineName = r.PipelineName.Trim(),
        Stage = r.Stage.Trim(),
        Amount = r.Amount,
        SaleDate = r.SaleDate,
        ExpectedCloseDate = r.ExpectedCloseDate,
        Notes = string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes.Trim(),
    };

    public static void Apply(UpdateSaleRequest r, Sale s)
    {
        s.CustomerId = r.CustomerId;
        s.PipelineName = r.PipelineName.Trim();
        s.Stage = r.Stage.Trim();
        s.Amount = r.Amount;
        s.SaleDate = r.SaleDate;
        s.ExpectedCloseDate = r.ExpectedCloseDate;
        s.Notes = string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes.Trim();
    }
}
