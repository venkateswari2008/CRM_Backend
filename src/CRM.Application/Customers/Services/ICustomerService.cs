using CRM.Application.Common;
using CRM.Application.Customers.Dtos;

namespace CRM.Application.Customers.Services;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> ListAsync(CustomerFilter filter, CancellationToken ct);

    Task<Result<CustomerDto>> GetByIdAsync(int id, CancellationToken ct);

    Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken ct);

    Task<Result<CustomerDto>> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken ct);

    Task<Result<bool>> DeleteAsync(int id, CancellationToken ct);
}
