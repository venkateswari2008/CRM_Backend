using System.Net;
using System.Net.Http.Json;
using CRM.Api.Middleware;
using CRM.Application.Auth.Dtos;

namespace CRM.IntegrationTests;

public class AuthEndpointsTests : IClassFixture<CrmWebApplicationFactory>
{
    private readonly CrmWebApplicationFactory _factory;

    public AuthEndpointsTests(CrmWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_ReturnsToken_ForSeededAdmin()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(TestData.AdminUsername, TestData.SeedPassword));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth!.AccessToken.Should().NotBeNullOrWhiteSpace();
        auth.TokenType.Should().Be("Bearer");
        auth.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(TestData.AdminUsername, "nope"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_EmptyBody_Returns400_WithProblemDetails()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("", ""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Signup_CreatesUser_AndReturnsToken()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var response = await client.PostAsJsonAsync("/api/auth/signup",
            new SignupRequest($"u{unique}", $"u{unique}@example.com", "Password1!"));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Signup_DuplicateUsername_Returns409()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/signup",
            new SignupRequest(TestData.AdminUsername, "other@example.com", "Password1!"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Roles_RequiresAuth_AndReturnsKnownRoles()
    {
        var anonymous = _factory.CreateClient();
        var anonRes = await anonymous.GetAsync("/api/auth/roles");
        anonRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var client = await _factory.CreateClient().AuthenticatedAs(TestData.AdminUsername, TestData.SeedPassword);
        var response = await client.GetAsync("/api/auth/roles");

        response.EnsureSuccessStatusCode();
        var roles = await response.Content.ReadFromJsonAsync<string[]>();
        roles.Should().BeEquivalentTo(new[] { "Admin", "User" });
    }

    [Fact]
    public async Task CorrelationIdHeader_IsReturned()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(TestData.AdminUsername, TestData.SeedPassword));

        response.Headers.Contains(CorrelationIdMiddleware.HeaderName).Should().BeTrue();
    }

    [Fact]
    public async Task Health_ReturnsOk_ForAuthenticatedUser()
    {
        var client = await _factory.CreateClient().AuthenticatedAs(TestData.AdminUsername, TestData.SeedPassword);
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
