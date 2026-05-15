using System.Text;
using System.Text.Json.Serialization;
using AspNetCoreRateLimit;
using CRM.Api.Common;
using CRM.Api.Extensions;
using CRM.Api.Filters;
using CRM.Api.Middleware;
using CRM.Application;
using CRM.Application.Abstractions;
using CRM.Application.Auth.Models;
using CRM.Domain.Enums;
using CRM.Infrastructure;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------------- Logging ----------------
builder.Host.UseSerilog((ctx, sp, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(sp)
    .Enrich.FromLogContext());

// ---------------- Forwarded headers ----------------
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

// ---------------- HttpContext + Current user ----------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// ---------------- Application + Infrastructure ----------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---------------- Controllers + JSON ----------------
builder.Services
    .AddControllers(opts =>
    {
        opts.Filters.Add<ValidationFilter>();
        opts.SuppressAsyncSuffixInActionNames = false;
    })
    .ConfigureApiBehaviorOptions(opts =>
    {
        opts.InvalidModelStateResponseFactory = ctx =>
            new BadRequestObjectResult(new ValidationProblemDetails(ctx.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Type = "https://httpstatuses.io/400",
                Instance = ctx.HttpContext.Request.Path
            });
    })
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// ---------------- API Versioning ----------------
builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCrmSwagger();
builder.Services.AddProblemDetails();

// ---------------- JWT Authentication ----------------
var jwt = builder.Configuration.GetSection(JwtSettings.SectionName)
          .Get<JwtSettings>()
          ?? throw new InvalidOperationException("Jwt section missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.RequireHttpsMetadata = true;
        opts.SaveToken = false;
        opts.MapInboundClaims = false;
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds),
            NameClaimType = "unique_name",
            RoleClaimType = "role"
        };
    });

// ---------------- Authorization (secure by default) ----------------
builder.Services
    .AddAuthorizationBuilder()
    .SetFallbackPolicy(
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build())
    .AddPolicy(AuthorizationPolicies.AdminOnly,
        p => p.RequireRole(UserRoles.Admin));

// ---------------- CORS ----------------
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p =>
        p.WithOrigins(allowedOrigins)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .WithExposedHeaders(CorrelationIdMiddleware.HeaderName)));

// ---------------- Rate Limiting ----------------
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(
    builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// ---------------- Health Checks ----------------
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// =================== PIPELINE ===================

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

// ✅ ENABLE SWAGGER IN ALL ENVIRONMENTS
app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseIpRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter
        .WriteHealthCheckUIResponse
});

// ---------------- DB migrate + seed ----------------
await using (var scope = app.Services.CreateAsyncScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<AppDbContext>();
    var hasher = sp.GetRequiredService<IPasswordHasher>();
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
    var adminPwd = builder.Configuration["Admin:SeedPassword"] ?? "ChangeMe!123";
    await DbSeeder.SeedAsync(db, hasher, logger, adminPwd);
}

try
{
    Log.Information("CRM API starting");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CRM API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;