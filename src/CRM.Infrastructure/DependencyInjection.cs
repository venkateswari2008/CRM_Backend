using CRM.Application.Abstractions;
using CRM.Application.Auth.Models;
using CRM.Infrastructure.Auth;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Interceptors;
using CRM.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .Validate(j => !string.IsNullOrWhiteSpace(j.SigningKey),
                "Jwt:SigningKey must be configured.")
            .ValidateOnStart();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<AuditingSaveChangesInterceptor>();

        var provider = configuration["Database:Provider"] ?? "Sqlite";
        var connection = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddDbContext<AppDbContext>((sp, opts) =>
        {
            var interceptor = sp.GetRequiredService<AuditingSaveChangesInterceptor>();

            if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                opts.UseSqlServer(connection, sql =>
                    sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
            }
            else
            {
                opts.UseSqlite(connection, sql =>
                    sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
            }

            opts.AddInterceptors(interceptor);
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return services;
    }
}
