using CRM.Api.Middleware;
using CRM.Domain.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRM.IntegrationTests.Unit;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int status, string body)> InvokeWithException(Exception toThrow, bool isDev = false)
    {
        var ctx = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        ctx.Request.Path = "/api/test";

        var env = new FakeEnv(isDev ? "Development" : "Production");
        var mw = new ExceptionHandlingMiddleware(
            _ => throw toThrow,
            NullLogger<ExceptionHandlingMiddleware>.Instance,
            env);

        await mw.InvokeAsync(ctx);

        ctx.Response.Body.Position = 0;
        using var reader = new StreamReader(ctx.Response.Body);
        var body = await reader.ReadToEndAsync();
        return (ctx.Response.StatusCode, body);
    }

    [Fact]
    public async Task Maps_EntityNotFound_To404()
    {
        var (status, body) = await InvokeWithException(new EntityNotFoundException("Customer", 1));
        status.Should().Be(404);
        body.Should().Contain("Resource not found");
    }

    [Fact]
    public async Task Maps_DuplicateEntity_To409()
    {
        var (status, _) = await InvokeWithException(new DuplicateEntityException("User", "Email", "a@b.c"));
        status.Should().Be(409);
    }

    [Fact]
    public async Task Maps_ValidationException_To400_AndIncludesErrorMap()
    {
        var failures = new[] { new ValidationFailure("Email", "is required") };
        var (status, body) = await InvokeWithException(new ValidationException(failures));
        status.Should().Be(400);
        body.Should().Contain("Validation failed");
        body.Should().Contain("Email");
    }

    [Fact]
    public async Task Maps_UnauthorizedAccess_To401()
    {
        var (status, _) = await InvokeWithException(new UnauthorizedAccessException());
        status.Should().Be(401);
    }

    [Fact]
    public async Task Maps_OperationCanceled_To499()
    {
        var (status, _) = await InvokeWithException(new OperationCanceledException());
        status.Should().Be(499);
    }

    [Fact]
    public async Task Unknown_Exception_Returns500_AndHidesDetailsInProduction()
    {
        var (status, body) = await InvokeWithException(new InvalidOperationException("internal secret"));
        status.Should().Be(500);
        body.Should().Contain("See server logs for details.");
        body.Should().NotContain("internal secret");
    }

    [Fact]
    public async Task Unknown_Exception_LeaksMessageInDevelopment()
    {
        var (status, body) = await InvokeWithException(new InvalidOperationException("dev-detail"), isDev: true);
        status.Should().Be(500);
        body.Should().Contain("dev-detail");
    }

    private sealed class FakeEnv : IHostEnvironment
    {
        public FakeEnv(string env) => EnvironmentName = env;
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = ".";
        public string EnvironmentName { get; set; }
    }
}
