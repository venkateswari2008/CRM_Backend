using CRM.Application.Abstractions;
using CRM.Infrastructure.Cache;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CRM.IntegrationTests;

/// <summary>
/// Boots the real Program.cs pipeline against an EF Core InMemory database. JWT signing key,
/// CORS, and rate limiting come from the in-memory configuration block below so tests don't
/// depend on appsettings or environment secrets.
/// </summary>
public sealed class CrmWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = "crm-test-" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Use values that match appsettings.json so the token issuer/audience/signing key
        // are identical regardless of which configuration source wins precedence.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=(localdb)\\unused;Database=unused",
                ["Jwt:Issuer"] = "crm.local",
                ["Jwt:Audience"] = "crm.client",
                ["Jwt:SigningKey"] = "kRxIgDw896glbSR5+NSIYOkoRRGmtCg6qoTves4kLEhBsU/Tj0F02TqAICXTm7un",
                ["Jwt:AccessTokenMinutes"] = "60",
                ["Jwt:ClockSkewSeconds"] = "5",
                ["Admin:SeedPassword"] = "ChangeMe!123",
                ["Cors:AllowedOrigins:0"] = "http://localhost:4200",
                ["IpRateLimiting:EnableEndpointRateLimiting"] = "false",
                ["IpRateLimiting:GeneralRules:0:Endpoint"] = "*",
                ["IpRateLimiting:GeneralRules:0:Period"] = "1m",
                ["IpRateLimiting:GeneralRules:0:Limit"] = "100000",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace the SQL Server DbContext with an InMemory one keyed per-factory so each
            // test fixture is isolated. We re-register the auditing interceptor too.
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>((sp, opts) =>
            {
                var interceptor = sp.GetRequiredService<AuditingSaveChangesInterceptor>();
                opts.UseInMemoryDatabase(DatabaseName);
                opts.AddInterceptors(interceptor);
            });

            services.RemoveAll<IApplicationDbContext>();
            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            // Force the no-op cache regardless of environment so tests don't reach for Redis.
            services.RemoveAll<ICacheService>();
            services.AddSingleton<ICacheService, NoOpCacheService>();

            // Test-server requests aren't HTTPS — the bearer middleware otherwise refuses tokens.
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                opts => opts.RequireHttpsMetadata = false);
        });
    }
}
