using System.Text.Json;
using CRM.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Middleware;

/// <summary>
/// Translates all uncaught exceptions into RFC 7807 <see cref="ProblemDetails"/> responses.
/// Never leaks stack traces or framework messages in non-Development environments.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private const int ClientClosedRequest = 499;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, title) = ex switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            DuplicateEntityException => (StatusCodes.Status409Conflict, "Duplicate resource"),
            DbUpdateException dbu when IsUniqueConstraintViolation(dbu)
                => (StatusCodes.Status409Conflict, "Duplicate resource"),
            DomainException => (StatusCodes.Status400BadRequest, "Domain rule violated"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            OperationCanceledException => (ClientClosedRequest, "Request cancelled"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        if (status >= 500)
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning(ex, "Handled exception on {Method} {Path}: {Message}",
                context.Request.Method, context.Request.Path, ex.Message);

        var detail = ex switch
        {
            DbUpdateException dbu when IsUniqueConstraintViolation(dbu)
                => "This change conflicts with an existing record on a unique field (e.g. email or username). Use a different value.",
            _ when _env.IsDevelopment() || status < 500 => ex.Message,
            _ => "See server logs for details.",
        };

        var problem = new ProblemDetails
        {
            Title = title,
            Status = status,
            Type = $"https://httpstatuses.io/{status}",
            Instance = context.Request.Path,
            Detail = detail,
        };

        if (ex is ValidationException ve)
        {
            problem.Extensions["errors"] = ve.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }

        if (context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var cid) && cid is not null)
            problem.Extensions["correlationId"] = cid.ToString();

        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(context.Response.Body, problem, JsonOptions);
    }

    // SQL Server error 2601 = unique-index violation, 2627 = primary-key/unique-constraint violation.
    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627);
}
