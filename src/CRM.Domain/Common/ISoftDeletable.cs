namespace CRM.Domain.Common;

/// <summary>Marker for entities that support soft deletion via a global query filter.</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }

    DateTimeOffset? DeletedAt { get; set; }

    int? DeletedBy { get; set; }
}
