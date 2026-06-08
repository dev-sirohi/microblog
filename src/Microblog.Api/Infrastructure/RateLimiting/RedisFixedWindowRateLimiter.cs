using System.Threading.RateLimiting;
using SystemRateLimiter = System.Threading.RateLimiting.RateLimiter;

namespace Microblog.Api.Infrastructure.RateLimiting;

/// <summary>
/// A Redis-backed fixed-window rate limiter compatible with ASP.NET Core's built-in rate limiting middleware.
/// All instances sharing the same Redis key share the same distributed counter.
/// </summary>
internal sealed class RedisFixedWindowRateLimiter : SystemRateLimiter
{
    private readonly IDatabase _db;
    private readonly string _key;
    private readonly int _permitLimit;
    private readonly TimeSpan _window;

    public RedisFixedWindowRateLimiter(IDatabase db, string key, int permitLimit, TimeSpan window)
    {
        _db = db;
        _key = key;
        _permitLimit = permitLimit;
        _window = window;
    }

    public override TimeSpan? IdleDuration => null;

    public override RateLimiterStatistics? GetStatistics() => new RateLimiterStatistics
    {
        CurrentAvailablePermits = _permitLimit,
        CurrentQueuedCount = 0,
        TotalFailedLeases = 0,
        TotalSuccessfulLeases = 0
    };

    protected override RateLimitLease AttemptAcquireCore(int permitCount) =>
        new DeniedLease(_window);

    protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken) =>
        new(AcquireInternalAsync());

    private async Task<RateLimitLease> AcquireInternalAsync()
    {
        long count = await _db.StringIncrementAsync(_key);
        if (count == 1)
            await _db.KeyExpireAsync(_key, _window);

        return count <= _permitLimit
            ? new AllowedLease()
            : new DeniedLease(_window);
    }

    protected override void Dispose(bool disposing) { }

    private sealed class AllowedLease : RateLimitLease
    {
        public override bool IsAcquired => true;
        public override IEnumerable<string> MetadataNames => [];
        public override bool TryGetMetadata(string metadataName, out object? metadata) { metadata = null; return false; }
        protected override void Dispose(bool disposing) { }
    }

    private sealed class DeniedLease(TimeSpan retryAfter) : RateLimitLease
    {
        public override bool IsAcquired => false;
        public override IEnumerable<string> MetadataNames => [MetadataName.RetryAfter.Name];
        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            if (metadataName == MetadataName.RetryAfter.Name) { metadata = retryAfter; return true; }
            metadata = null;
            return false;
        }
        protected override void Dispose(bool disposing) { }
    }
}
