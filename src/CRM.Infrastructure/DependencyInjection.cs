using CRM.Application.Abstractions;
using CRM.Application.Auth.Models;
using CRM.Infrastructure.Auth;
using CRM.Infrastructure.Cache;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Interceptors;
using CRM.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace CRM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment env)
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

        var connection = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddDbContext<AppDbContext>((sp, opts) =>
        {
            var interceptor = sp.GetRequiredService<AuditingSaveChangesInterceptor>();

            opts.UseSqlServer(connection, sql =>
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

            opts.AddInterceptors(interceptor);
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        if (env.IsDevelopment())
        {
            // Corp network blocks outbound 6380 → use a no-op cache so the API stays fast.
            services.AddSingleton<ICacheService, NoOpCacheService>();
        }
        else
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var settings = configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>()
                    ?? throw new InvalidOperationException("Redis section is not configured.");

                var options = new ConfigurationOptions
                {
                    EndPoints = { $"{settings.Host}:{settings.Port}" },
                    Password = settings.Password,
                    Ssl = settings.Ssl,
                    AbortOnConnectFail = false,
                };
                return ConnectionMultiplexer.Connect(options);
            });

            services.AddSingleton<ICacheService, RedisCacheService>();
        }

        return services;
    }
}
