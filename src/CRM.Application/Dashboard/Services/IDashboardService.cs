using CRM.Application.Dashboard.Dtos;

namespace CRM.Application.Dashboard.Services;

public interface IDashboardService
{
    Task<DashboardDto> GetOverviewAsync(int? year, CancellationToken ct);
}
