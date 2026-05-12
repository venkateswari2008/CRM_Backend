using CRM.Application.Common;
using CRM.Application.Sales.Dtos;

namespace CRM.Application.Sales.Services;

public interface ISaleService
{
    Task<PagedResult<SaleDto>> ListAsync(SaleFilter filter, CancellationToken ct);

    Task<Result<SaleDto>> GetByIdAsync(int id, CancellationToken ct);

    Task<Result<SaleDto>> CreateAsync(CreateSaleRequest request, CancellationToken ct);

    Task<Result<SaleDto>> UpdateAsync(int id, UpdateSaleRequest request, CancellationToken ct);

    Task<Result<bool>> DeleteAsync(int id, CancellationToken ct);

    Task<Result<byte[]>> ExportCsvAsync(SaleFilter filter, CancellationToken ct);
}
