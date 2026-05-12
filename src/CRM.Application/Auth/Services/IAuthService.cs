using CRM.Application.Auth.Dtos;
using CRM.Application.Common;

namespace CRM.Application.Auth.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct);

    Task<Result<AuthResponse>> SignupAsync(SignupRequest request, CancellationToken ct);

    IReadOnlyList<string> GetRoles();
}
