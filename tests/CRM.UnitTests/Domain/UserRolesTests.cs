using CRM.Domain.Enums;

namespace CRM.UnitTests.Domain;

public class UserRolesTests
{
    [Fact]
    public void All_ExposesAdminAndUser()
    {
        UserRoles.All.Should().BeEquivalentTo(new[] { "Admin", "User" });
    }

    [Theory]
    [InlineData("Admin", true)]
    [InlineData("User", true)]
    [InlineData("admin", false)]
    [InlineData("Guest", false)]
    [InlineData("", false)]
    public void IsValid_MatchesExactNames(string? role, bool expected)
    {
        UserRoles.IsValid(role).Should().Be(expected);
    }

    [Fact]
    public void IsValid_RejectsNull()
    {
        UserRoles.IsValid(null).Should().BeFalse();
    }
}
