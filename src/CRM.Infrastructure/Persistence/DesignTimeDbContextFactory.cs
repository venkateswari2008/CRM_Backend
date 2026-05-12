using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CRM.Infrastructure.Persistence;

/// <summary>
/// Used only by the <c>dotnet ef</c> tooling to construct the DbContext at design time
/// (e.g. to generate migrations). The runtime registration in API DI is the source of truth.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var provider = Environment.GetEnvironmentVariable("CRM_DB_PROVIDER") ?? "Sqlite";
        var connection = Environment.GetEnvironmentVariable("CRM_DB_CONNECTION")
            ?? "Data Source=crm-design.db";

        var builder = new DbContextOptionsBuilder<AppDbContext>();

        if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            builder.UseSqlServer(connection, sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
        else
            builder.UseSqlite(connection, sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

        return new AppDbContext(builder.Options);
    }
}
