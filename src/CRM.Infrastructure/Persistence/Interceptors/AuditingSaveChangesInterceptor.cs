using CRM.Application.Abstractions;
using CRM.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CRM.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps <see cref="AuditableEntity"/> and <see cref="ISoftDeletable"/> entities with
/// created/updated/deleted metadata on every SaveChanges. Translates a hard delete on
/// an <see cref="ISoftDeletable"/> into a soft delete.
/// </summary>
public sealed class AuditingSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUser _currentUser;

    public AuditingSaveChangesInterceptor(IDateTimeProvider clock, ICurrentUser currentUser)
    {
        _clock = clock;
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null) return;

        var now = _clock.UtcNow;
        var userId = _currentUser.UserId;

        foreach (EntityEntry entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditableEntity auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditable.CreatedAt = now;
                        auditable.CreatedBy ??= userId;
                        break;
                    case EntityState.Modified:
                        auditable.UpdatedAt = now;
                        auditable.UpdatedBy = userId;
                        entry.Property(nameof(AuditableEntity.CreatedAt)).IsModified = false;
                        entry.Property(nameof(AuditableEntity.CreatedBy)).IsModified = false;
                        break;
                }
            }

            if (entry.Entity is ISoftDeletable softDeletable)
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    softDeletable.IsDeleted = true;
                    softDeletable.DeletedAt = now;
                    softDeletable.DeletedBy = userId;
                }
                else if (entry.State == EntityState.Modified && softDeletable.IsDeleted &&
                         softDeletable.DeletedAt is null)
                {
                    softDeletable.DeletedAt = now;
                    softDeletable.DeletedBy = userId;
                }
            }
        }
    }
}
