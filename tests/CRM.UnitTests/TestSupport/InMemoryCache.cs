using System.Collections.Concurrent;
using System.Text.Json;
using CRM.Application.Abstractions;

namespace CRM.UnitTests.TestSupport;

/// <summary>
/// Simple, deterministic in-memory cache used to exercise the cache code paths in services
/// without bringing in Redis or MemoryCache eviction policies.
/// </summary>
internal sealed class InMemoryCache : ICacheService
{
    private readonly ConcurrentDictionary<string, string> _store = new();
    public int Hits { get; private set; }
    public int Misses { get; private set; }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (_store.TryGetValue(key, out var json))
        {
            Hits++;
            return Task.FromResult(JsonSerializer.Deserialize<T>(json));
        }
        Misses++;
        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        _store[key] = JsonSerializer.Serialize(value);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        foreach (var key in _store.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
            _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public int Count => _store.Count;
}
