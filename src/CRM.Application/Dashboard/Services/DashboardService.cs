using CRM.Application.Abstractions;
using CRM.Application.Dashboard.Dtos;
using CRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CRM.Application.Dashboard.Services;

public sealed class DashboardService : IDashboardService
{
    private const int TopCustomerCount = 5;
    private const int RecentLeadsDays = 30;

    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public DashboardService(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<DashboardDto> GetOverviewAsync(int? year, CancellationToken ct)
    {
        var targetYear = year ?? _clock.Today.Year;
        var yearStart = new DateOnly(targetYear, 1, 1);
        var yearEnd = new DateOnly(targetYear, 12, 31);

        // Materialise once — a CRM dataset is small enough that an in-memory
        // roll-up is the right trade-off and keeps the projection logic readable.
        var sales = await _db.Sales.AsNoTracking()
            .Where(s => s.SaleDate >= yearStart && s.SaleDate <= yearEnd)
            .Select(s => new
            {
                s.CustomerId,
                s.Stage,
                s.Amount,
                s.SaleDate,
                CustomerFirstName = s.Customer.FirstName,
                CustomerLastName = s.Customer.LastName,
                CustomerCompany = s.Customer.Company,
            })
            .ToListAsync(ct);

        var won = sales.Where(s => s.Stage == SaleStages.ClosedWon).ToList();
        var lost = sales.Count(s => s.Stage == SaleStages.ClosedLost);
        var open = sales.Count(s =>
            s.Stage != SaleStages.ClosedWon && s.Stage != SaleStages.ClosedLost);

        var totalSales = won.Sum(s => s.Amount);

        var since = _clock.Today.AddDays(-RecentLeadsDays);
        var sinceUtc = since.ToDateTime(TimeOnly.MinValue).ToUniversalTime();
        var sinceTicks = new DateTimeOffset(sinceUtc, TimeSpan.Zero).UtcTicks;

        var newLeads = await _db.Customers.AsNoTracking()
            .CountAsync(c => c.CreatedAt >= new DateTimeOffset(sinceUtc, TimeSpan.Zero), ct);

        var monthly = won
            .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
            .Select(g => new MonthlySalesDto(g.Key.Year, g.Key.Month, g.Sum(x => x.Amount), g.Count()))
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToList();

        var stageBreakdown = sales
            .GroupBy(s => s.Stage)
            .Select(g => new StageBreakdownDto(g.Key, g.Sum(x => x.Amount), g.Count()))
            .ToList();

        var topCustomers = won
            .GroupBy(s => new { s.CustomerId, s.CustomerFirstName, s.CustomerLastName, s.CustomerCompany })
            .Select(g => new TopCustomerDto(
                g.Key.CustomerId,
                ($"{g.Key.CustomerFirstName} {g.Key.CustomerLastName}").Trim(),
                g.Key.CustomerCompany,
                g.Sum(x => x.Amount),
                g.Count()))
            .OrderByDescending(c => c.Total)
            .Take(TopCustomerCount)
            .ToList();

        return new DashboardDto(
            totalSales,
            newLeads,
            open,
            won.Count,
            lost,
            monthly,
            stageBreakdown,
            topCustomers);
    }
}
