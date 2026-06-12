using System.Security.Claims;
using CRM.Api.Common;
using Microsoft.AspNetCore.Http;

namespace CRM.IntegrationTests.Unit;

public class CurrentUserTests
{
    private static HttpContextAccessor BuildAccessor(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        var ctx = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        return new HttpContextAccessor { HttpContext = ctx };
    }

    [Fact]
    public void UserId_FromSubClaim()
    {
        var accessor = BuildAccessor(new Claim("sub", "42"));
        new CurrentUser(accessor).UserId.Should().Be(42);
    }

    [Fact]
    public void UserId_FromNameIdentifier_TakesPriority()
    {
        var accessor = BuildAccessor(
            new Claim(ClaimTypes.NameIdentifier, "7"),
            new Claim("sub", "42"));
        new CurrentUser(accessor).UserId.Should().Be(7);
    }

    [Fact]
    public void Username_ReadsFromIdentityName()
    {
        var accessor = BuildAccessor(new Claim(ClaimTypes.Name, "alice"));
        new CurrentUser(accessor).Username.Should().Be("alice");
    }

    [Fact]
    public void Role_ReadsRoleClaim_AndIsInRoleHonoursIt()
    {
        var accessor = BuildAccessor(new Claim(ClaimTypes.Role, "Admin"));
        var current = new CurrentUser(accessor);

        current.Role.Should().Be("Admin");
        current.IsInRole("Admin").Should().BeTrue();
        current.IsInRole("User").Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_FalseWhenNoIdentity()
    {
        var ctx = new DefaultHttpContext(); // identity not authenticated
        var accessor = new HttpContextAccessor { HttpContext = ctx };
        new CurrentUser(accessor).IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void Returns_NullForMissingClaims()
    {
        var accessor = BuildAccessor();
        var current = new CurrentUser(accessor);
        current.UserId.Should().BeNull();
        current.Username.Should().BeNull();
        current.Role.Should().BeNull();
    }
}
