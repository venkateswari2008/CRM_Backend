using System.Text;
using CRM.Application.Common;
using CRM.Application.Sales.Dtos;
using CRM.Application.Sales.Services;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRM.UnitTests.Services;

public class SaleServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly FakeCurrentUser _currentUser = new();
    private readonly SaleService _sut;
    private int _customerId;
    private int _userId;
    private static readonly DateOnly Today = new(2026, 6, 12);

    public SaleServiceTests()
    {
        _db = InMemoryDbContextFactory.Create(currentUser: _currentUser);
        _sut = new SaleService(_db, _currentUser, NullLogger<SaleService>.Instance);
        SeedBaseline().GetAwaiter().GetResult();
    }

    public void Dispose() => _db.Dispose();

    private async Task SeedBaseline()
    {
        var user = new User { Username = "owner", Email = "owner@crm.local", PasswordHash = "h", Role = "User" };
        var cust = new Customer { FirstName = "Jane", LastName = "Doe", Email = "jane@example.com", Company = "Acme" };
        _db.Users.Add(user);
        _db.Customers.Add(cust);
        await _db.SaveChangesAsync();
        _userId = user.Id;
        _customerId = cust.Id;
        _currentUser.UserId = _userId;
    }

    private CreateSaleRequest Create(string stage = "Proposal", decimal amount = 1000m, DateOnly? date = null) =>
        new(_customerId, null, "Enterprise", stage, amount, date ?? Today, null, "n");

    [Fact]
    public async Task Create_AssignsCurrentUserAsOwner()
    {
        var r = await _sut.CreateAsync(Create(), default);

        r.IsSuccess.Should().BeTrue();
        r.Value!.UserId.Should().Be(_userId);
        r.Value.UserName.Should().Be("owner");
    }

    [Fact]
    public async Task Create_StampsActualCloseDate_WhenStageIsClosedWon()
    {
        var r = await _sut.CreateAsync(Create(stage: SaleStages.ClosedWon), default);

        r.IsSuccess.Should().BeTrue();
        r.Value!.ActualCloseDate.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_LeavesActualCloseDateNull_WhenStageIsOpen()
    {
        var r = await _sut.CreateAsync(Create(stage: SaleStages.Proposal), default);

        r.IsSuccess.Should().BeTrue();
        r.Value!.ActualCloseDate.Should().BeNull();
    }

    [Fact]
    public async Task Create_ReturnsNotFound_WhenCustomerMissing()
    {
        var r = await _sut.CreateAsync(Create() with { CustomerId = 9999 }, default);

        r.IsSuccess.Should().BeFalse();
        r.ErrorCode.Should().Be(ResultErrorCodes.NotFound);
    }

    [Fact]
    public async Task Create_ReturnsNotFound_WhenExplicitUserMissing()
    {
        var req = Create() with { UserId = 9999 };

        var r = await _sut.CreateAsync(req, default);

        r.IsSuccess.Should().BeFalse();
        r.ErrorCode.Should().Be(ResultErrorCodes.NotFound);
    }

    [Fact]
    public async Task Create_ThrowsWhenNoUserContextAvailable()
    {
        _currentUser.UserId = null;
        var req = Create() with { UserId = null };

        Func<Task> act = () => _sut.CreateAsync(req, default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNotFound()
    {
        var r = await _sut.GetByIdAsync(404, default);
        r.IsSuccess.Should().BeFalse();
        r.ErrorCode.Should().Be(ResultErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetById_LoadsNavigationProperties()
    {
        var created = (await _sut.CreateAsync(Create(), default)).Value!;
        var r = await _sut.GetByIdAsync(created.Id, default);

        r.IsSuccess.Should().BeTrue();
        r.Value!.CustomerName.Should().Be("Jane Doe");
        r.Value.Company.Should().Be("Acme");
    }

    [Fact]
    public async Task Update_NotFound_ReturnsNotFound()
    {
        var req = new UpdateSaleRequest(_customerId, "X", "Proposal", 1, Today, null, null);
        var r = await _sut.UpdateAsync(404, req, default);
        r.ErrorCode.Should().Be(ResultErrorCodes.NotFound);
    }

    [Fact]
    public async Task Update_NewCustomerMustExist()
    {
        var created = (await _sut.CreateAsync(Create(), default)).Value!;
        var req = new UpdateSaleRequest(9999, "X", "Proposal", 1, Today, null, null);

        var r = await _sut.UpdateAsync(created.Id, req, default);

        r.IsSuccess.Should().BeFalse();
        r.ErrorCode.Should().Be(ResultErrorCodes.NotFound);
    }

    [Fact]
    public async Task Update_ToClosedStage_StampsActualCloseDate()
    {
        var created = (await _sut.CreateAsync(Create(stage: SaleStages.Proposal), default)).Value!;

        var r = await _sut.UpdateAsync(created.Id,
            new UpdateSaleRequest(_customerId, "Enterprise", SaleStages.ClosedWon, 1000, Today, null, null), default);

        r.IsSuccess.Should().BeTrue();
        r.Value!.ActualCloseDate.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_ReopenedStage_ClearsActualCloseDate()
    {
        var created = (await _sut.CreateAsync(Create(stage: SaleStages.ClosedWon), default)).Value!;

        var r = await _sut.UpdateAsync(created.Id,
            new UpdateSaleRequest(_customerId, "Enterprise", SaleStages.Proposal, 1000, Today, null, null), default);

        r.IsSuccess.Should().BeTrue();
        r.Value!.ActualCloseDate.Should().BeNull();
    }

    [Fact]
    public async Task Delete_SoftDeletes()
    {
        var created = (await _sut.CreateAsync(Create(), default)).Value!;
        var r = await _sut.DeleteAsync(created.Id, default);
        r.IsSuccess.Should().BeTrue();

        var lookup = await _sut.GetByIdAsync(created.Id, default);
        lookup.IsSuccess.Should().BeFalse();
        lookup.ErrorCode.Should().Be(ResultErrorCodes.NotFound);
    }

    [Fact]
    public async Task Delete_NotFound_ReturnsNotFound()
    {
        var r = await _sut.DeleteAsync(404, default);
        r.IsSuccess.Should().BeFalse();
        r.ErrorCode.Should().Be(ResultErrorCodes.NotFound);
    }

    [Fact]
    public async Task List_AppliesAllFilters()
    {
        await _sut.CreateAsync(Create(stage: "Proposal", amount: 100, date: Today.AddDays(-10)), default);
        await _sut.CreateAsync(Create(stage: "Negotiation", amount: 500, date: Today), default);
        await _sut.CreateAsync(Create(stage: "ClosedWon", amount: 2000, date: Today.AddDays(-5)), default);

        var page = await _sut.ListAsync(new SaleFilter
        {
            CustomerId = _customerId,
            UserId = _userId,
            Stage = "Negotiation",
            FromDate = Today.AddDays(-1),
            ToDate = Today.AddDays(1),
            MinAmount = 100,
            MaxAmount = 1000,
            Search = "enterprise",
            PageSize = 20,
        }, default);

        page.Items.Should().ContainSingle(s => s.Stage == "Negotiation");
    }

    [Theory]
    [InlineData("amount")]
    [InlineData("-amount")]
    [InlineData("date")]
    [InlineData("-date")]
    [InlineData(null)]
    public async Task List_AcceptsAllKnownSortKeys(string? sort)
    {
        await _sut.CreateAsync(Create(amount: 100), default);
        await _sut.CreateAsync(Create(amount: 500), default);

        var page = await _sut.ListAsync(new SaleFilter { Sort = sort, PageSize = 20 }, default);

        page.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExportCsv_ContainsHeadersAndOneRowPerSale()
    {
        await _sut.CreateAsync(Create(amount: 100), default);
        await _sut.CreateAsync(Create(amount: 200), default);

        var r = await _sut.ExportCsvAsync(new SaleFilter { PageSize = 50 }, default);

        r.IsSuccess.Should().BeTrue();
        var csv = Encoding.UTF8.GetString(r.Value!);
        csv.Should().Contain("SaleId");
        csv.Should().Contain("Jane Doe");
        csv.Should().Contain("100.00");
        csv.Should().Contain("200.00");
        // 1 header + 2 data rows (final newline allowed)
        csv.Trim().Split('\n').Length.Should().Be(3);
    }
}
