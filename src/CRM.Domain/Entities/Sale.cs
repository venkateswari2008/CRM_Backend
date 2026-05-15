using CRM.Domain.Common;

namespace CRM.Domain.Entities;

/// <summary>
/// A sales pipeline deal associated with a customer and owned by a user.
/// Tracks the stage progression and monetary value of the opportunity.
/// </summary>
public class Sale : AuditableEntity, ISoftDeletable
{
    public int CustomerId { get; set; }

    public int UserId { get; set; }

    public string PipelineName { get; set; } = string.Empty;

    public string Stage { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateOnly SaleDate { get; set; }

    public DateOnly? ExpectedCloseDate { get; set; }

    /// <summary>
    /// Stamped automatically when <see cref="Stage"/> moves into <see cref="Enums.SaleStages.ClosedStages"/>
    /// and cleared when the deal is reopened. Never set directly by the client.
    /// </summary>
    public DateOnly? ActualCloseDate { get; set; }

    public string? Notes { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public Customer Customer { get; set; } = null!;

    public User User { get; set; } = null!;
}
