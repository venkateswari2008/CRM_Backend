using CRM.Application.Dashboard.Services;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRM.UnitTests.Services;

public class DashboardServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly FakeDateTimeProvider _clock = new(new DateTimeOffset(2026, 6, 12, 0, 0, 0, TimeSpan.Zero));
    private readonly InMemoryCache _cache = new();
    private readonly DashboardService _sut;
    private int _userId;

    private readonly string _dbName = Guid.NewGuid().ToString("N");

    public DashboardServiceTests()
    {
        // SUT uses a context WITH the auditing interceptor (production-like).
        _db = InMemoryDbContextFactory.Create(dbName: _dbName, clock: _clock);
        _sut = new DashboardService(_db, _clock, _cache, NullLogger<DashboardService>.Instance);
        Seed().GetAwaiter().GetResult();
    }

    public void Dispose() => _db.Dispose();

    private async Task Seed()
    {
        // Seed via a *raw* context (no interceptor) so we can set CreatedAt to a past date.
        // The auditing interceptor unconditionally overwrites CreatedAt on Add.
        using var seedCtx = InMemoryDbContextFactory.Create(
            dbName: _dbName, clock: _clock, withAuditingInterceptor: false);

        var user = new User { Username = "u", Email = "u@x.y", PasswordHash = "h", Role = "User" };
        seedCtx.Users.Add(user);
        await seedCtx.SaveChangesAsync();
        _userId = user.Id;

        var oldCust = new Customer
        {
            FirstName = "Old", LastName = "Lead", Email = "old@x.y", Company = "AlphaCo",
            CreatedAt = _clock.UtcNow.AddDays(-60),
        };
        var newCust = new Customer
        {
            FirstName = "New", LastName = "Lead", Email = "new@x.y", Company = "BetaCo",
            CreatedAt = _clock.UtcNow.AddDays(-5),
        };
        seedCtx.Customers.AddRange(oldCust, newCust);
        await seedCtx.SaveChangesAsync();

        seedCtx.Sales.AddRange(
            new Sale { CustomerId = oldCust.Id, UserId = _userId, PipelineName = "P", Stage = SaleStages.ClosedWon,
                Amount = 1000, SaleDate = new DateOnly(2026, 1, 15) },
            new Sale { CustomerId = oldCust.Id, UserId = _userId, PipelineName = "P", Stage = SaleStages.ClosedWon,
                Amount = 2000, SaleDate = new DateOnly(2026, 3, 1) },
            new Sale { CustomerId = newCust.Id, UserId = _userId, PipelineName = "P", Stage = SaleStages.ClosedLost,
                Amount = 500, SaleDate = new DateOnly(2026, 4, 10) },
            new Sale { CustomerId = newCust.Id, UserId = _userId, PipelineName = "P", Stage = SaleStages.Proposal,
                Amount = 750, SaleDate = new DateOnly(2026, 5, 1) },
            // Out-of-year sale, should be ignored
            new Sale { CustomerId = newCust.Id, UserId = _userId, PipelineName = "P", Stage = SaleStages.ClosedWon,
                Amount = 9000, SaleDate = new DateOnly(2025, 12, 31) });

        await seedCtx.SaveChangesAsync();
    }

    [Fact]
    public async Task GetOverview_AggregatesYearScopedTotals()
    {
        var dto = await _sut.GetOverviewAsync(2026, default);

        dto.TotalSales.Should().Be(3000m);          // 1000 + 2000
        dto.WonDeals.Should().Be(2);
        dto.LostDeals.Should().Be(1);
        dto.OpenOpportunities.Should().Be(1);
        dto.MonthlySales.Should().HaveCount(2);     // Jan + Mar
        dto.StageBreakdown.Should().NotBeEmpty();
        dto.TopCustomers.Should().NotBeEmpty();
        dto.TopCustomers.First().Total.Should().Be(3000m);
    }

    [Fact]
    public async Task GetOverview_CountsNewLeadsInLast30Days()
    {
        var dto = await _sut.GetOverviewAsync(2026, default);
        dto.NewLeads.Should().Be(1);
    }

    [Fact]
    public async Task GetOverview_DefaultsToClockYear_WhenNoYearProvided()
    {
        var dto = await _sut.GetOverviewAsync(null, default);
        dto.TotalSales.Should().Be(3000m);
    }

    [Fact]
    public async Task GetOverview_CacheHitOnSecondCall()
    {
        await _sut.GetOverviewAsync(2026, default);
        var hitsBefore = _cache.Hits;
        await _sut.GetOverviewAsync(2026, default);
        _cache.Hits.Should().BeGreaterThan(hitsBefore);
    }
}
