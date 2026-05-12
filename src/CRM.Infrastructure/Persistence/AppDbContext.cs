using CRM.Application.Abstractions;
using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Sale> Sales => Set<Sale>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // SQLite cannot ORDER BY DateTimeOffset natively; store as UTC ticks instead.
        if (Database.IsSqlite())
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var prop in entity.GetProperties())
                {
                    if (prop.ClrType == typeof(DateTimeOffset))
                        prop.SetValueConverter(new DateTimeOffsetToTicksConverter());
                    else if (prop.ClrType == typeof(DateTimeOffset?))
                        prop.SetValueConverter(new NullableDateTimeOffsetToTicksConverter());
                }
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite has no native DateOnly support; map via converter.
        configurationBuilder.Properties<DateOnly>().HaveConversion<DateOnlyConverter>();
        configurationBuilder.Properties<DateOnly?>().HaveConversion<NullableDateOnlyConverter>();
        base.ConfigureConventions(configurationBuilder);
    }
}
