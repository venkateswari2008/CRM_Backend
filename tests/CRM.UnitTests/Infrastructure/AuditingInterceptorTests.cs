using CRM.Domain.Entities;
using CRM.Infrastructure.Persistence;
using CRM.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace CRM.UnitTests.Infrastructure;

public class AuditingInterceptorTests
{
    [Fact]
    public async Task SaveChanges_StampsCreatedAtAndCreatedBy_OnNewEntities()
    {
        var clock = new FakeDateTimeProvider();
        var current = new FakeCurrentUser { UserId = 7 };
        using var db = InMemoryDbContextFactory.Create(clock: clock, currentUser: current);

        db.Customers.Add(new Customer { FirstName = "A", LastName = "B", Email = "a@b.c" });
        await db.SaveChangesAsync();

        var stored = await db.Customers.SingleAsync();
        stored.CreatedAt.Should().Be(clock.UtcNow);
        stored.CreatedBy.Should().Be(7);
    }

    [Fact]
    public async Task SaveChanges_StampsUpdatedAt_OnModifications_AndPreservesCreated()
    {
        var clock = new FakeDateTimeProvider();
        var current = new FakeCurrentUser { UserId = 7 };
        using var db = InMemoryDbContextFactory.Create(clock: clock, currentUser: current);

        var c = new Customer { FirstName = "A", LastName = "B", Email = "a@b.c" };
        db.Customers.Add(c);
        await db.SaveChangesAsync();
        var originalCreated = c.CreatedAt;

        clock.UtcNow = clock.UtcNow.AddMinutes(5);
        current.UserId = 9;
        c.LastName = "C";
        await db.SaveChangesAsync();

        c.UpdatedAt.Should().Be(clock.UtcNow);
        c.UpdatedBy.Should().Be(9);
        c.CreatedAt.Should().Be(originalCreated);
    }

    [Fact]
    public async Task Remove_OnSoftDeletableEntity_TranslatesToSoftDelete()
    {
        var clock = new FakeDateTimeProvider();
        var current = new FakeCurrentUser { UserId = 11 };
        using var db = InMemoryDbContextFactory.Create(clock: clock, currentUser: current);

        var c = new Customer { FirstName = "A", LastName = "B", Email = "a@b.c" };
        db.Customers.Add(c);
        await db.SaveChangesAsync();

        db.Customers.Remove(c);
        await db.SaveChangesAsync();

        // EF Core's query filter on !IsDeleted means the row should disappear from default queries.
        (await db.Customers.CountAsync()).Should().Be(0);

        // But the row physically exists with IsDeleted=true.
        var raw = await db.Customers.IgnoreQueryFilters().SingleAsync();
        raw.IsDeleted.Should().BeTrue();
        raw.DeletedAt.Should().NotBeNull();
        raw.DeletedBy.Should().Be(11);
    }
}
