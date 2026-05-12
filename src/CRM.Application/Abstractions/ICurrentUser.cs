namespace CRM.Application.Abstractions;

/// <summary>
/// Provides access to the currently-authenticated user. Implemented in the API layer
/// using <c>IHttpContextAccessor</c>; mocked in tests.
/// </summary>
public interface ICurrentUser
{
    int? UserId { get; }

    string? Username { get; }

    string? Role { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string role);
}
