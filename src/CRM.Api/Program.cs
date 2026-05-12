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
using CRM.Infrastructure;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------- Logging (Serilog) ----------
builder.Host.UseSerilog((ctx, sp, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(sp)
    .Enrich.FromLogContext());

// ---------- Forwarded headers (for reverse proxies / containers) ----------
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

// ---------- HttpContext access + current user ----------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// ---------- Application + Infrastructure ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---------- MVC + JSON ----------
builder.Services
    .AddControllers(opts =>
    {
        opts.Filters.Add<ValidationFilter>();
        opts.SuppressAsyncSuffixInActionNames = false;
    })
    .ConfigureApiBehaviorOptions(opts =>
    {
        // Surface model-binding failures as RFC 7807 ProblemDetails.
        opts.InvalidModelStateResponseFactory = ctx => new BadRequestObjectResult(
            new ValidationProblemDetails(ctx.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Type = "https://httpstatuses.io/400",
                Instance = ctx.HttpContext.Request.Path,
            });
    })
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// ---------- API versioning ----------
builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCrmSwagger();
builder.Services.AddProblemDetails();

// ---------- JWT authentication ----------
var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt section missing from configuration.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds),
            NameClaimType = "unique_name",
            RoleClaimType = "role",
        };
    });

builder.Services.AddAuthorization();

// ---------- CORS ----------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders(CorrelationIdMiddleware.HeaderName)));

// ---------- Rate limiting ----------
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// ---------- Health checks ----------
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// ---------- Build & pipeline ----------
var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
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
    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse,
});

// ---------- DB migrate + seed ----------
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

/// <summary>Exposed so WebApplicationFactory in the integration tests can target this assembly.</summary>
public partial class Program;
