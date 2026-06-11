using CRM.Application.Abstractions;

namespace CRM.Infrastructure.Cache;

// Used in Development where the corp network blocks outbound 6380 — keeps the API
// fast (no 5s Redis timeout per request) while still satisfying ICacheService DI.
public sealed class NoOpCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) =>
        Task.FromResult(default(T));

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default) =>
        Task.CompletedTask;
}
