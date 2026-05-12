using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.Application.Abstractions;

/// <summary>
/// DbContext abstraction exposed to the Application layer. Keeps Application
/// independent of EF Core implementation details while still allowing IQueryable
/// composition for performant, paginated queries.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }

    DbSet<Customer> Customers { get; }

    DbSet<Sale> Sales { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
