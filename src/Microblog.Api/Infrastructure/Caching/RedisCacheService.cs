using System.Text.Json;

namespace Microblog.Api.Infrastructure.Caching;

public sealed class RedisCacheService(
    IConnectionMultiplexer mux,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);
    private readonly IDatabase _db = mux.GetDatabase();

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null) return cached;

        var value = await factory();
        await SetAsync(key, value, ttl, ct);
        return value;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        try
        {
            string json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, ttl ?? DefaultTtl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache set failed for key {Key}", key);
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            RedisValue raw = await _db.StringGetAsync(key);
            if (raw.IsNullOrEmpty)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>((string)raw!);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache get failed for key {Key}", key);
            return default;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try { await _db.KeyDeleteAsync(key); }
        catch (Exception ex) { logger.LogWarning(ex, "Cache remove failed for key {Key}", key); }
    }
}
