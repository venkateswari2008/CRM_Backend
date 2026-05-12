using Microsoft.OpenApi.Models;

namespace CRM.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddCrmSwagger(this IServiceCollection services) =>
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "CRM API",
                Version = "v1",
                Description = "Customer Relationship Management API — Cognizant Case Study",
                Contact = new OpenApiContact { Name = "CRM Team", Email = "crm@cognizant.local" },
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT token issued by /api/auth/login. Paste only the token (no 'Bearer ' prefix).",
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                    },
                    Array.Empty<string>()
                },
            });

            c.EnableAnnotations();
        });
}
