using System.Net;
using System.Net.Http.Json;
using CRM.Application.Dashboard.Dtos;

namespace CRM.IntegrationTests;

public class DashboardEndpointTests : IClassFixture<CrmWebApplicationFactory>
{
    private readonly CrmWebApplicationFactory _factory;
    public DashboardEndpointTests(CrmWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Dashboard_RequiresAuth()
    {
        var anon = _factory.CreateClient();
        var resp = await anon.GetAsync("/api/dashboard");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Dashboard_AsAuthenticated_ReturnsOverview()
    {
        var client = await _factory.CreateClient().AuthenticatedAs(TestData.AdminUsername, TestData.SeedPassword);
        var resp = await client.GetAsync("/api/dashboard?year=2026");

        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<DashboardDto>();
        dto.Should().NotBeNull();
        dto!.MonthlySales.Should().NotBeNull();
        dto.StageBreakdown.Should().NotBeNull();
        dto.TopCustomers.Should().NotBeNull();
    }
}
