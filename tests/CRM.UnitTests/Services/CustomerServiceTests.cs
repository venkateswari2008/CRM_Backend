using CRM.Application.Common;
using CRM.Application.Customers.Dtos;
using CRM.Application.Customers.Services;
using CRM.Domain.Entities;
using CRM.Infrastructure.Persistence;
using CRM.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRM.UnitTests.Services;

public class CustomerServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly InMemoryCache _cache = new();
    private readonly CustomerService _sut;

    public CustomerServiceTests()
    {
        _db = InMemoryDbContextFactory.Create();
        _sut = new CustomerService(_db, _cache, NullLogger<CustomerService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    private static CreateCustomerRequest BuildCreate(string email = "jane@example.com") =>
        new("Jane", "Doe", email, "(123) 456-7890",
            "1 Elm St", "Boston", "MA", "02101", "USA", "Acme", "vip");

    private async Task<Customer> SeedAsync(string email, string first = "Jane", string last = "Doe", string? company = null)
    {
        var c = new Customer
        {
            FirstName = first,
            LastName = last,
            Email = email,
            Company = company,
        };
        _db.Customers.Add(c);
        await _db.SaveChangesAsync();
        return c;
    }

    [Fact]
    public async Task Create_PersistsCustomer_AndInvalidatesListCache()
    {
        await _cache.SetAsync("customer:list:any", "stale");
        var result = await _sut.CreateAsync(BuildCreate(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Email.Should().Be("jane@example.com");
        _cache.Count.Should().Be(0);
    }

    [Fact]
    public async Task Create_DuplicateEmail_IsCaseInsensitive_AndReturnsDuplicate()
    {
        await SeedAsync("dup@example.com");

        var result = await _sut.CreateAsync(BuildCreate(email: "DUP@example.com"), default);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ResultErrorCodes.Duplicate);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var result = await _sut.GetByIdAsync(999, default);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ResultErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetById_PrimesCacheAndReturnsFromCacheNextTime()
    {
        var c = await SeedAsync("a@b.c");

        await _sut.GetByIdAsync(c.Id, default);
        var miss = _cache.Misses;

        var second = await _sut.GetByIdAsync(c.Id, default);

        second.IsSuccess.Should().BeTrue();
        _cache.Hits.Should().BeGreaterThan(0);
        _cache.Misses.Should().Be(miss);
    }

    [Fact]
    public async Task Update_DuplicateEmailOnOtherCustomer_ReturnsDuplicate()
    {
        var taken = await SeedAsync("taken@example.com");
        var target = await SeedAsync("target@example.com");

        var req = new UpdateCustomerRequest(
            "Updated", "Person", taken.Email, null, null, null, null, null, null, null, null);

        var result = await _sut.UpdateAsync(target.Id, req, default);
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ResultErrorCodes.Duplicate);
    }

    [Fact]
    public async Task Update_SameEmailDifferentCase_IsAllowed()
    {
        var target = await SeedAsync("Target@Example.com");

        var req = new UpdateCustomerRequest(
            "Updated", "Person", "TARGET@example.com", null, null, "NYC", null, null, null, null, null);

        var result = await _sut.UpdateAsync(target.Id, req, default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.City.Should().Be("NYC");
    }

    [Fact]
    public async Task Update_NotFound_ReturnsNotFound()
    {
        var req = new UpdateCustomerRequest(
            "X", "Y", "x@y.z", null, null, null, null, null, null, null, null);
        var result = await _sut.UpdateAsync(404, req, default);
        result.ErrorCode.Should().Be(ResultErrorCodes.NotFound);
    }

    [Fact]
    public async Task Delete_NotFound_ReturnsNotFound()
    {
        var result = await _sut.DeleteAsync(404, default);
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ResultErrorCodes.NotFound);
    }

    [Fact]
    public async Task Delete_BlocksWhenCustomerHasActiveSales()
    {
        var c = await SeedAsync("hassales@example.com");
        var u = new User { Username = "owner", Email = "o@x.y", PasswordHash = "h", Role = "User" };
        _db.Users.Add(u);
        await _db.SaveChangesAsync();
        _db.Sales.Add(new Sale
        {
            CustomerId = c.Id,
            UserId = u.Id,
            PipelineName = "X",
            Stage = "Proposal",
            Amount = 1,
            SaleDate = new DateOnly(2026, 1, 1),
        });
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(c.Id, default);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ResultErrorCodes.Conflict);
    }

    [Fact]
    public async Task Delete_SoftDeletes_AndDropsFromList()
    {
        var c = await SeedAsync("delete@example.com");

        var result = await _sut.DeleteAsync(c.Id, default);

        result.IsSuccess.Should().BeTrue();
        var paged = await _sut.ListAsync(new CustomerFilter { PageSize = 50 }, default);
        paged.Items.Should().NotContain(x => x.Id == c.Id);
    }

    [Fact]
    public async Task List_AppliesSearchAndFilters_AndPaging()
    {
        await SeedAsync("alpha@a.com", first: "Alpha", company: "Acme");
        await SeedAsync("beta@a.com", first: "Beta", company: "Acme");
        await SeedAsync("gamma@a.com", first: "Gamma", company: "Globex");

        var filtered = await _sut.ListAsync(new CustomerFilter
        {
            Company = "Acme",
            Search = "alp",
            PageSize = 10,
        }, default);

        filtered.TotalCount.Should().Be(1);
        filtered.Items.Single().FirstName.Should().Be("Alpha");
    }

    [Theory]
    [InlineData("name")]
    [InlineData("-name")]
    [InlineData("email")]
    [InlineData("-email")]
    [InlineData("created")]
    [InlineData("-created")]
    [InlineData(null)]
    public async Task List_HonoursAllSortKeys(string? sort)
    {
        await SeedAsync("alpha@a.com", first: "Alpha");
        await SeedAsync("beta@b.com", first: "Beta");

        var page = await _sut.ListAsync(new CustomerFilter { Sort = sort, PageSize = 10 }, default);

        page.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_ServesFromCacheOnSecondCall()
    {
        await SeedAsync("a@a.com");
        var filter = new CustomerFilter { PageSize = 10 };

        await _sut.ListAsync(filter, default);
        var hitsBefore = _cache.Hits;
        await _sut.ListAsync(filter, default);

        _cache.Hits.Should().BeGreaterThan(hitsBefore);
    }
}
