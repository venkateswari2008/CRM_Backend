using System.Net;
using System.Net.Http.Json;
using CRM.Application.Common;
using CRM.Application.Customers.Dtos;

namespace CRM.IntegrationTests;

[Collection("Sequential")]
public class CustomerEndpointsTests : IClassFixture<CrmWebApplicationFactory>
{
    private readonly CrmWebApplicationFactory _factory;
    public CustomerEndpointsTests(CrmWebApplicationFactory factory) => _factory = factory;

    private Task<HttpClient> AdminClientAsync() =>
        _factory.CreateClient().AuthenticatedAs(TestData.AdminUsername, TestData.SeedPassword);

    private Task<HttpClient> DemoUserClientAsync() =>
        _factory.CreateClient().AuthenticatedAs(TestData.DemoUsername, TestData.SeedPassword);

    private static CreateCustomerRequest NewCustomer(string suffix) =>
        new(
            "Test", $"User{suffix}", $"new-{suffix}@example.com",
            "(555) 123-4567", "1 Test St", "Boston", "MA", "02101", "USA", "TestCo", "via tests");

    [Fact]
    public async Task List_RequiresAuth()
    {
        var anon = _factory.CreateClient();
        var resp = await anon.GetAsync("/api/customers");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullLifecycle_Create_Read_Update_Delete()
    {
        var admin = await AdminClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        // Create
        var createResp = await admin.PostAsJsonAsync("/api/customers", NewCustomer(suffix));
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<CustomerDto>();
        created.Should().NotBeNull();
        created!.Id.Should().BeGreaterThan(0);

        // Read
        var getResp = await admin.GetAsync($"/api/customers/{created.Id}");
        getResp.EnsureSuccessStatusCode();
        var fetched = await getResp.Content.ReadFromJsonAsync<CustomerDto>();
        fetched!.Email.Should().Be(created.Email);

        // Update (PUT)
        var updateReq = new UpdateCustomerRequest(
            "Updated", $"User{suffix}", created.Email,
            "(555) 999-9999", null, "NYC", null, null, null, null, null);
        var updateResp = await admin.PutAsJsonAsync($"/api/customers/{created.Id}", updateReq);
        updateResp.EnsureSuccessStatusCode();
        var updated = await updateResp.Content.ReadFromJsonAsync<CustomerDto>();
        updated!.FirstName.Should().Be("Updated");
        updated.City.Should().Be("NYC");

        // Delete
        var deleteResp = await admin.DeleteAsync($"/api/customers/{created.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // After delete -> 404
        var afterDelete = await admin.GetAsync($"/api/customers/{created.Id}");
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ValidationFailure_Returns400()
    {
        var admin = await AdminClientAsync();
        var bad = new CreateCustomerRequest(
            "", "", "not-an-email", null, null, null, null, null, null, null, null);

        var resp = await admin.PostAsJsonAsync("/api/customers", bad);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_DuplicateEmail_Returns409()
    {
        var admin = await AdminClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var first = await admin.PostAsJsonAsync("/api/customers", NewCustomer(suffix));
        first.EnsureSuccessStatusCode();

        var dup = await admin.PostAsJsonAsync("/api/customers", NewCustomer(suffix));
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_AsNonAdmin_Returns403()
    {
        var admin = await AdminClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var created = await (await admin.PostAsJsonAsync("/api/customers", NewCustomer(suffix)))
            .Content.ReadFromJsonAsync<CustomerDto>();

        var user = await DemoUserClientAsync();
        var resp = await user.DeleteAsync($"/api/customers/{created!.Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_AsNonAdmin_Returns403()
    {
        var admin = await AdminClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var created = await (await admin.PostAsJsonAsync("/api/customers", NewCustomer(suffix)))
            .Content.ReadFromJsonAsync<CustomerDto>();

        var user = await DemoUserClientAsync();
        var req = new UpdateCustomerRequest(
            "X", "Y", created!.Email, null, null, null, null, null, null, null, null);
        var resp = await user.PutAsJsonAsync($"/api/customers/{created.Id}", req);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_ReturnsPagedResult()
    {
        var admin = await AdminClientAsync();
        var resp = await admin.GetAsync("/api/customers?pageSize=5");

        resp.EnsureSuccessStatusCode();
        var page = await resp.Content.ReadFromJsonAsync<PagedResult<CustomerDto>>();
        page.Should().NotBeNull();
        page!.PageSize.Should().Be(5);
        page.Items.Should().NotBeNull();
    }
}
