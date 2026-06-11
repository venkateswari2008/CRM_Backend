using System.Text.Json;
using CRM.Application.Abstractions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CRM.Infrastructure.Cache;

public sealed class RedisCacheService : ICacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    private readonly IConnectionMultiplexer _multiplexer;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer multiplexer, ILogger<RedisCacheService> logger)
    {
        _multiplexer = multiplexer;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var json = await _multiplexer.GetDatabase().StringGetAsync(key);
            if (json.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>((string)json!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache GET failed for key {Key}; falling back to source", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _multiplexer.GetDatabase().StringSetAsync(key, json, ttl ?? DefaultTtl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache SET failed for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _multiplexer.GetDatabase().KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE failed for key {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        try
        {
            var db = _multiplexer.GetDatabase();
            foreach (var ep in _multiplexer.GetEndPoints())
            {
                var server = _multiplexer.GetServer(ep);
                await foreach (var key in server.KeysAsync(pattern: $"{prefix}*").WithCancellation(ct))
                {
                    await db.KeyDeleteAsync(key);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE-BY-PREFIX failed for prefix {Prefix}", prefix);
        }
    }
}
