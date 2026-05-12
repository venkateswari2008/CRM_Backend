namespace CRM.Domain.Common;

/// <summary>
/// Base class for all persistent entities. Provides a strongly-typed primary key
/// and a concurrency token for optimistic concurrency control.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }

    /// <summary>Row version used by EF Core for optimistic concurrency.</summary>
    public byte[]? RowVersion { get; set; }
}
