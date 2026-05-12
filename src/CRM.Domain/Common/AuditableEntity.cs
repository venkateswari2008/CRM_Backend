namespace CRM.Domain.Common;

/// <summary>
/// Entity that tracks creation and modification metadata. Values are populated
/// by an EF Core SaveChanges interceptor in the Infrastructure layer.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTimeOffset CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }
}
