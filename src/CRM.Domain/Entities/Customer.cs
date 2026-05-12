using CRM.Domain.Common;

namespace CRM.Domain.Entities;

/// <summary>
/// Represents a customer / contact owned by the CRM. Uniqueness is enforced
/// on <see cref="Email"/> across active (non-deleted) records.
/// </summary>
public class Customer : AuditableEntity, ISoftDeletable
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? AddressLine { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? ZipCode { get; set; }

    public string? Country { get; set; }

    public string? Company { get; set; }

    public string? Notes { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();

    public string FullName => string.IsNullOrWhiteSpace(LastName)
        ? FirstName
        : $"{FirstName} {LastName}".Trim();
}
