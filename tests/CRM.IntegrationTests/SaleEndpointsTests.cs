using System.Net;
using System.Net.Http.Json;
using CRM.Application.Common;
using CRM.Application.Customers.Dtos;
using CRM.Application.Sales.Dtos;

namespace CRM.IntegrationTests;

[Collection("Sequential")]
public class SaleEndpointsTests : IClassFixture<CrmWebApplicationFactory>
{
    private readonly CrmWebApplicationFactory _factory;
    public SaleEndpointsTests(CrmWebApplicationFactory factory) => _factory = factory;

    private Task<HttpClient> AdminClientAsync() =>
        _factory.CreateClient().AuthenticatedAs(TestData.AdminUsername, TestData.SeedPassword);

    private Task<HttpClient> DemoUserClientAsync() =>
        _factory.CreateClient().AuthenticatedAs(TestData.DemoUsername, TestData.SeedPassword);

    private static async Task<CustomerDto> SeedCustomerAsync(HttpClient admin)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var req = new CreateCustomerRequest(
            "Sale", $"Customer{suffix}", $"sale-{suffix}@example.com",
            null, null, null, null, null, null, "SaleCo", null);
        var resp = await admin.PostAsJsonAsync("/api/customers", req);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CustomerDto>())!;
    }

    [Fact]
    public async Task FullLifecycle_Create_Get_Update_Delete()
    {
        var admin = await AdminClientAsync();
        var customer = await SeedCustomerAsync(admin);

        var create = new CreateSaleRequest(
            customer.Id, null, "Enterprise", "Proposal", 1234.50m,
            new DateOnly(2026, 5, 1), new DateOnly(2026, 6, 1), "first deal");

        var createResp = await admin.PostAsJsonAsync("/api/sales", create);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<SaleDto>();
        created.Should().NotBeNull();

        var fetched = await (await admin.GetAsync($"/api/sales/{created!.Id}"))
            .Content.ReadFromJsonAsync<SaleDto>();
        fetched!.CustomerName.Should().Contain("Sale");

        var update = new UpdateSaleRequest(
            customer.Id, "Enterprise", "ClosedWon", 1500m,
            new DateOnly(2026, 5, 1), null, "won");
        var updateResp = await admin.PutAsJsonAsync($"/api/sales/{created.Id}", update);
        updateResp.EnsureSuccessStatusCode();
        var updated = await updateResp.Content.ReadFromJsonAsync<SaleDto>();
        updated!.Stage.Should().Be("ClosedWon");
        updated.ActualCloseDate.Should().NotBeNull();

        var deleteResp = await admin.DeleteAsync($"/api/sales/{created.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_AsNonAdmin_Returns403()
    {
        var admin = await AdminClientAsync();
        var customer = await SeedCustomerAsync(admin);
        var created = await (await admin.PostAsJsonAsync("/api/sales", new CreateSaleRequest(
            customer.Id, null, "Mid-market", "Proposal", 100m,
            new DateOnly(2026, 4, 1), null, null)))
            .Content.ReadFromJsonAsync<SaleDto>();

        var user = await DemoUserClientAsync();
        var resp = await user.DeleteAsync($"/api/sales/{created!.Id}");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Export_AsAdmin_ReturnsCsv()
    {
        var admin = await AdminClientAsync();
        var customer = await SeedCustomerAsync(admin);
        await admin.PostAsJsonAsync("/api/sales", new CreateSaleRequest(
            customer.Id, null, "Pipeline", "Proposal", 500m,
            new DateOnly(2026, 5, 5), null, null));

        var resp = await admin.GetAsync("/api/sales/export");
        resp.EnsureSuccessStatusCode();
        resp.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("SaleId");
        body.Should().Contain("Pipeline");
    }

    [Fact]
    public async Task Export_AsNonAdmin_Returns403()
    {
        var user = await DemoUserClientAsync();
        var resp = await user.GetAsync("/api/sales/export");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_NonExistent_Returns404()
    {
        var admin = await AdminClientAsync();
        var resp = await admin.GetAsync("/api/sales/9999999");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_UnknownCustomer_Returns404()
    {
        var admin = await AdminClientAsync();
        var req = new CreateSaleRequest(9999999, null, "X", "Proposal", 1, new DateOnly(2026, 1, 1), null, null);
        var resp = await admin.PostAsJsonAsync("/api/sales", req);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_PagedResult_ShapeOk()
    {
        var admin = await AdminClientAsync();
        var resp = await admin.GetAsync("/api/sales?pageSize=5");
        resp.EnsureSuccessStatusCode();
        var page = await resp.Content.ReadFromJsonAsync<PagedResult<SaleDto>>();
        page!.PageSize.Should().Be(5);
    }
}
