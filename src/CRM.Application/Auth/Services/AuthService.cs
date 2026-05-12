using CRM.Application.Abstractions;
using CRM.Application.Auth.Dtos;
using CRM.Application.Common;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Application.Auth.Services;

public sealed class AuthService : IAuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IDateTimeProvider clock,
        ILogger<AuthService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var input = request.UsernameOrEmail.Trim().ToLowerInvariant();

        var user = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.Username.ToLower() == input || u.Email.ToLower() == input, ct);

        if (user is null)
        {
            // Run the hasher on a dummy value to keep response time constant and
            // mitigate user-enumeration via timing analysis.
            _passwordHasher.Verify(request.Password, "$2a$11$abcdefghijklmnopqrstuv");
            _logger.LogInformation("Login failed: unknown user {Input}", input);
            return Result<AuthResponse>.Failure("Invalid credentials.", ResultErrorCodes.Unauthorized);
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login blocked: user {UserId} is inactive", user.Id);
            return Result<AuthResponse>.Failure("Account is inactive.", ResultErrorCodes.Forbidden);
        }

        if (user.LockedOutUntil is { } lockedUntil && lockedUntil > _clock.UtcNow)
        {
            _logger.LogWarning("Login blocked: user {UserId} is locked out until {Until}", user.Id, lockedUntil);
            return Result<AuthResponse>.Failure("Account is temporarily locked.", ResultErrorCodes.Forbidden);
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockedOutUntil = _clock.UtcNow.Add(LockoutDuration);
                _logger.LogWarning("User {UserId} locked out after {Attempts} failed attempts",
                    user.Id, user.FailedLoginAttempts);
            }

            await _db.SaveChangesAsync(ct);
            return Result<AuthResponse>.Failure("Invalid credentials.", ResultErrorCodes.Unauthorized);
        }

        user.FailedLoginAttempts = 0;
        user.LockedOutUntil = null;
        user.LastLoginAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);

        var token = _tokenGenerator.Generate(user);
        _logger.LogInformation("User {UserId} ({Username}) logged in", user.Id, user.Username);

        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id, user.Username, user.Email, user.Role,
            token.AccessToken, token.TokenType, token.ExpiresAt));
    }

    public async Task<Result<AuthResponse>> SignupAsync(SignupRequest request, CancellationToken ct)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(u =>
            u.Username.ToLower() == username.ToLower() || u.Email.ToLower() == email, ct);

        if (exists)
        {
            return Result<AuthResponse>.Failure(
                "A user with that username or email already exists.", ResultErrorCodes.Duplicate);
        }

        var role = string.IsNullOrWhiteSpace(request.Role) ? UserRoles.User : request.Role!;

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = role,
            IsActive = true,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var token = _tokenGenerator.Generate(user);
        _logger.LogInformation("User {UserId} ({Username}) registered with role {Role}",
            user.Id, user.Username, user.Role);

        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id, user.Username, user.Email, user.Role,
            token.AccessToken, token.TokenType, token.ExpiresAt));
    }

    public IReadOnlyList<string> GetRoles() => UserRoles.All;
}
