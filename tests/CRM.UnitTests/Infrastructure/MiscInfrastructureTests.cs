using CRM.Infrastructure.Cache;
using CRM.Infrastructure.Time;

namespace CRM.UnitTests.Infrastructure;

public class MiscInfrastructureTests
{
    [Fact]
    public void SystemDateTimeProvider_ReturnsCurrentUtc()
    {
        var p = new SystemDateTimeProvider();

        p.UtcNow.Offset.Should().Be(TimeSpan.Zero);
        var diff = (DateTimeOffset.UtcNow - p.UtcNow).Duration();
        diff.Should().BeLessThan(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void SystemDateTimeProvider_TodayMatchesUtc()
    {
        var p = new SystemDateTimeProvider();
        p.Today.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    [Fact]
    public async Task NoOpCacheService_GetAlwaysReturnsDefault()
    {
        var cache = new NoOpCacheService();
        await cache.SetAsync("k", "v");
        var got = await cache.GetAsync<string>("k");
        got.Should().BeNull();
    }

    [Fact]
    public async Task NoOpCacheService_RemoveOpsAreNoThrow()
    {
        var cache = new NoOpCacheService();
        await cache.RemoveAsync("k");
        await cache.RemoveByPrefixAsync("p:");
        // pass if no exception
    }
}
