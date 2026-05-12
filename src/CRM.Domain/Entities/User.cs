using CRM.Domain.Common;

namespace CRM.Domain.Entities;

/// <summary>
/// System user. Authenticates against a BCrypt-hashed password and is granted
/// permissions through a role (Admin or User).
/// </summary>
public class User : AuditableEntity, ISoftDeletable
{
    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? LastLoginAt { get; set; }

    public int FailedLoginAttempts { get; set; }

    public DateTimeOffset? LockedOutUntil { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public ICollection<Sale> OwnedSales { get; set; } = new List<Sale>();
}
