namespace Microblog.Api.Infrastructure.Caching;

/// <summary>
/// Generic cache-aside service with stampede protection via distributed locking.
/// All methods are no-throw — on Redis failure the factory result is returned uncached.
/// </summary>
public interface ICacheService
{
    /// <summary>Returns the cached value for <paramref name="key"/>, or executes <paramref name="factory"/> and caches the result.</summary>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null, CancellationToken ct = default);

    /// <summary>Writes a value to the cache.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default);

    /// <summary>Reads a value from the cache; returns <c>default</c> on miss.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>Deletes a single key.</summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>Deletes all keys associated with <paramref name="tag"/> (tag-based invalidation).</summary>
    Task RemoveByTagAsync(string tag, CancellationToken ct = default);

    /// <summary>Associates <paramref name="key"/> with <paramref name="tag"/> so it can be bulk-invalidated.</summary>
    Task TagKeyAsync(string key, string tag, CancellationToken ct = default);
}
