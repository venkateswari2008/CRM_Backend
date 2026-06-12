using CRM.Application.Abstractions;
using CRM.Domain.Common;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace CRM.UnitTests.TestSupport;

internal static class InMemoryDbContextFactory
{
    public static AppDbContext Create(
        string? dbName = null,
        IDateTimeProvider? clock = null,
        ICurrentUser? currentUser = null,
        bool withAuditingInterceptor = true)
    {
        clock ??= new FakeDateTimeProvider();
        currentUser ??= new FakeCurrentUser();

        var optsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString("N"))
            .EnableSensitiveDataLogging();

        if (withAuditingInterceptor)
            optsBuilder.AddInterceptors(new AuditingSaveChangesInterceptor(clock, currentUser));

        var ctx = new AppDbContext(optsBuilder.Options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    /// <summary>
    /// Stamps newly added auditable entities directly so seeding tests don't rely on the
    /// interceptor (helpful when constructing fixture data deterministically).
    /// </summary>
    public static void StampCreated(AppDbContext db, DateTimeOffset when)
    {
        foreach (var entry in db.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditableEntity ae && entry.State == EntityState.Added)
                ae.CreatedAt = when;
        }
    }
}
