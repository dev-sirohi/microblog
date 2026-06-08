using RedLockNet;
using System.Text.Json;

namespace Microblog.Api.Infrastructure.Caching;

public sealed class RedisCacheService(
    IConnectionMultiplexer mux,
    IDistributedLockFactory redLockFactory,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LockWait = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan LockRetry = TimeSpan.FromMilliseconds(200);
    private readonly IDatabase _db = mux.GetDatabase();

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null) return cached;

        // Distributed lock prevents cache stampede when many requests miss simultaneously
        string lockKey = $"lock:{key}";
        try
        {
            using var redLock = await redLockFactory.CreateLockAsync(lockKey, LockExpiry, LockWait, LockRetry, ct);
            if (!redLock.IsAcquired)
            {
                // Another instance is computing — return a cache check (may still be a miss)
                return await GetAsync<T>(key, ct);
            }

            // Double-check after acquiring lock
            cached = await GetAsync<T>(key, ct);
            if (cached is not null) return cached;

            var value = await factory();
            await SetAsync(key, value, ttl, ct);
            return value;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache lock failed for key {Key}; executing factory without caching", key);
            return await factory();
        }
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
            if (raw.IsNullOrEmpty) return default;
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

    public async Task RemoveByTagAsync(string tag, CancellationToken ct = default)
    {
        try
        {
            string tagSetKey = TagSetKey(tag);
            var members = await _db.SetMembersAsync(tagSetKey);
            if (members.Length == 0) return;

            var keys = members.Select(m => (RedisKey)(string)m!).Append((RedisKey)tagSetKey).ToArray();
            await _db.KeyDeleteAsync(keys);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache remove-by-tag failed for tag {Tag}", tag);
        }
    }

    public async Task TagKeyAsync(string key, string tag, CancellationToken ct = default)
    {
        try
        {
            string tagSetKey = TagSetKey(tag);
            await _db.SetAddAsync(tagSetKey, key);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache tag failed for key {Key} tag {Tag}", key, tag);
        }
    }

    private static string TagSetKey(string tag) => $"cachetag:{tag}";
}
