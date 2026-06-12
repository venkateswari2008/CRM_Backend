using CRM.Application.Auth.Dtos;
using CRM.Application.Auth.Services;
using CRM.Application.Common;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRM.UnitTests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly FakePasswordHasher _hasher = new();
    private readonly FakeJwtTokenGenerator _tokens = new();
    private readonly FakeDateTimeProvider _clock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _db = InMemoryDbContextFactory.Create();
        _sut = new AuthService(_db, _hasher, _tokens, _clock, NullLogger<AuthService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    private async Task<User> SeedUserAsync(
        string username = "admin",
        string password = "ChangeMe!123",
        string role = "Admin",
        bool isActive = true,
        DateTimeOffset? lockedUntil = null,
        int failedAttempts = 0)
    {
        var user = new User
        {
            Username = username,
            Email = $"{username}@crm.local",
            PasswordHash = _hasher.Hash(password),
            Role = role,
            IsActive = isActive,
            LockedOutUntil = lockedUntil,
            FailedLoginAttempts = failedAttempts,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Login_ByUsername_ReturnsTokenAndResetsCounters()
    {
        var user = await SeedUserAsync(failedAttempts: 3);

        var result = await _sut.LoginAsync(new LoginRequest("admin", "ChangeMe!123"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().StartWith("fake-token-");
        result.Value.Username.Should().Be("admin");
        result.Value.TokenType.Should().Be("Bearer");

        var refreshed = await _db.Users.FirstAsync(u => u.Id == user.Id);
        refreshed.FailedLoginAttempts.Should().Be(0);
        refreshed.LastLoginAt.Should().Be(_clock.UtcNow);
    }

    [Fact]
    public async Task Login_ByEmail_AlsoWorks()
    {
        await SeedUserAsync(username: "ada");

        var result = await _sut.LoginAsync(new LoginRequest("ada@crm.local", "ChangeMe!123"), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Login_UnknownUser_ReturnsUnauthorized_AndCallsHasherToMitigateTiming()
    {
        var result = await _sut.LoginAsync(new LoginRequest("ghost", "whatever"), default);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ResultErrorCodes.Unauthorized);
        _hasher.VerifyCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Login_InactiveUser_ReturnsForbidden()
    {
        await SeedUserAsync(isActive: false);

        var result = await _sut.LoginAsync(new LoginRequest("admin", "ChangeMe!123"), default);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ResultErrorCodes.Forbidden);
    }

    [Fact]
    public async Task Login_LockedOutUser_ReturnsForbidden()
    {
        await SeedUserAsync(lockedUntil: _clock.UtcNow.AddMinutes(5));

        var result = await _sut.LoginAsync(new LoginRequest("admin", "ChangeMe!123"), default);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ResultErrorCodes.Forbidden);
    }

    [Fact]
    public async Task Login_WrongPassword_IncrementsCounter_AndLocksAfter5Attempts()
    {
        var user = await SeedUserAsync(failedAttempts: 4);

        var result = await _sut.LoginAsync(new LoginRequest("admin", "WRONG!"), default);

        result.IsSuccess.Should().BeFalse();
        var refreshed = await _db.Users.FirstAsync(u => u.Id == user.Id);
        refreshed.FailedLoginAttempts.Should().Be(5);
        refreshed.LockedOutUntil.Should().NotBeNull();
        refreshed.LockedOutUntil!.Value.Should().Be(_clock.UtcNow.AddMinutes(15));
    }

    [Fact]
    public async Task Login_PriorLockoutExpired_AllowsSuccessfulLogin()
    {
        await SeedUserAsync(lockedUntil: _clock.UtcNow.AddMinutes(-1));

        var result = await _sut.LoginAsync(new LoginRequest("admin", "ChangeMe!123"), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Signup_CreatesUser_WithDefaultUserRole()
    {
        var result = await _sut.SignupAsync(
            new SignupRequest("alice", "Alice@example.com", "Password1!"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be(UserRoles.User);
        result.Value.Email.Should().Be("alice@example.com");

        var stored = await _db.Users.FirstAsync(u => u.Username == "alice");
        stored.Email.Should().Be("alice@example.com");
        stored.PasswordHash.Should().StartWith(FakePasswordHasher.Prefix);
    }

    [Fact]
    public async Task Signup_DuplicateUsername_ReturnsDuplicate()
    {
        await SeedUserAsync(username: "alice");

        var result = await _sut.SignupAsync(
            new SignupRequest("alice", "alice2@example.com", "Password1!"), default);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ResultErrorCodes.Duplicate);
    }

    [Fact]
    public async Task Signup_DuplicateEmail_IsCaseInsensitive_AndReturnsDuplicate()
    {
        await SeedUserAsync(username: "alice");

        var result = await _sut.SignupAsync(
            new SignupRequest("alice2", "ALICE@crm.local", "Password1!"), default);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ResultErrorCodes.Duplicate);
    }

    [Fact]
    public async Task Signup_HonoursExplicitAdminRole()
    {
        var result = await _sut.SignupAsync(
            new SignupRequest("root", "root@crm.local", "Password1!", UserRoles.Admin), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be(UserRoles.Admin);
    }

    [Fact]
    public void GetRoles_ReturnsKnownRoles()
    {
        _sut.GetRoles().Should().BeEquivalentTo(UserRoles.All);
    }
}
