using CRM.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace CRM.IntegrationTests.Unit;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task UsesProvidedHeader_WhenPresent()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[CorrelationIdMiddleware.HeaderName] = "abc-123";

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(ctx);

        ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().Should().Be("abc-123");
        ctx.Items[CorrelationIdMiddleware.HeaderName].Should().Be("abc-123");
    }

    [Fact]
    public async Task GeneratesId_WhenHeaderMissing()
    {
        var ctx = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(ctx);

        ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().Should().NotBeNullOrEmpty();
        var generated = ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        generated.Length.Should().Be(32); // GUID "N" format
    }
}
