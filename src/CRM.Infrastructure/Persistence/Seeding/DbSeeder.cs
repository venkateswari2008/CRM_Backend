using CRM.Application.Abstractions;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Persistence.Seeding;

public static class DbSeeder
{
    /// <summary>
    /// Seeds the initial admin user and a small set of demo data. Idempotent —
    /// safe to invoke on every startup.
    /// </summary>
    public static async Task SeedAsync(
        AppDbContext db,
        IPasswordHasher hasher,
        ILogger logger,
        string adminPassword,
        CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (!await db.Users.AnyAsync(ct))
        {
            db.Users.Add(new User
            {
                Username = "admin",
                Email = "admin@crm.local",
                PasswordHash = hasher.Hash(adminPassword),
                Role = UserRoles.Admin,
                IsActive = true,
            });
            db.Users.Add(new User
            {
                Username = "demo.user",
                Email = "demo@crm.local",
                PasswordHash = hasher.Hash(adminPassword),
                Role = UserRoles.User,
                IsActive = true,
            });
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded default users (admin, demo.user)");
        }

        if (!await db.Customers.AnyAsync(ct))
        {
            db.Customers.AddRange(
                new Customer { FirstName = "John", LastName = "Smith", Email = "john.smith@acmecorp.com",
                    Phone = "(123) 456-7890", Company = "Acme Corp", City = "Anytown", Country = "USA" },
                new Customer { FirstName = "Emma", LastName = "Johnson", Email = "emma@globalservices.com",
                    Phone = "(555) 222-1111", Company = "Global Services", City = "Boston", Country = "USA" },
                new Customer { FirstName = "Michael", LastName = "Brown", Email = "m.brown@smithco.com",
                    Phone = "(555) 333-2222", Company = "Smith & Co", City = "Chicago", Country = "USA" },
                new Customer { FirstName = "Olivia", LastName = "Davis", Email = "olivia@greensolutions.com",
                    Phone = "(555) 444-3333", Company = "Green Solutions", City = "Portland", Country = "USA" });

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded demo customers");
        }
    }
}
