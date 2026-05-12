using CRM.Application.Common;

namespace CRM.Application.Sales.Dtos;

public sealed class SaleFilter : PageRequest
{
    public int? CustomerId { get; set; }

    public int? UserId { get; set; }

    public string? Stage { get; set; }

    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public decimal? MinAmount { get; set; }

    public decimal? MaxAmount { get; set; }
}
