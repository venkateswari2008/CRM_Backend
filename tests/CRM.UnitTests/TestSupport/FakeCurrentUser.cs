using CRM.Application.Abstractions;

namespace CRM.UnitTests.TestSupport;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public int? UserId { get; set; } = 1;
    public string? Username { get; set; } = "test.user";
    public string? Role { get; set; } = "User";
    public bool IsAuthenticated => UserId.HasValue;
    public bool IsInRole(string role) => string.Equals(Role, role, StringComparison.Ordinal);
}
