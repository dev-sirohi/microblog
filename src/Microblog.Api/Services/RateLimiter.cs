using System.Security.Claims;

namespace Microblog.Api.Services;

/// <summary>
/// Redis-backed <b>sliding-window log</b> rate limiter.
///
/// For every request we keep a Redis sorted set per (action, caller) where the score of
/// each member is the request timestamp (Unix milliseconds). On each call we:
///   1. drop every entry older than <c>now - window</c>  (ZREMRANGEBYSCORE),
///   2. count what remains inside the window            (ZCARD),
///   3. reject if the count already reached the limit,  otherwise
///   4. record this request                             (ZADD) and refresh the key TTL.
///
/// Unlike a fixed window (a single INCR that resets on a clock boundary and lets a caller
/// burst 2x the limit across the boundary), this counts the *last N seconds continuously*,
/// so the limit is enforced smoothly at every instant. The counter lives in Redis, so it is
/// shared across every API instance.
/// </summary>
public sealed class RateLimiter : IRateLimiter
{
    private static readonly DateTime Epoch = DateTime.UnixEpoch;

    private readonly string _callerKey;
    private readonly IDatabase _inMemoryDb;

    public RateLimiter(IConnectionMultiplexer connectionMultiplexer, IHttpContextAccessor httpContextAccessor)
    {
        _inMemoryDb = connectionMultiplexer.GetDatabase();

        var context = httpContextAccessor.HttpContext;
        long userId = 0;
        if (context?.User?.Identity?.IsAuthenticated ?? false)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long uid)) userId = uid;
        }

        // Authenticated callers are limited per user; anonymous callers per client IP.
        if (userId > 0)
        {
            _callerKey = $"userId:{userId}";
        }
        else
        {
            string? clientIp = context?.Connection.RemoteIpAddress?.ToString();
            _callerKey = string.IsNullOrWhiteSpace(clientIp) ? "clientIp:unknown" : $"clientIp:{clientIp}";
        }
    }

    public async Task<bool> IsRequestAllowedAsync(AppConstants.ApiRequestAction requestType)
    {
        var (limit, window) = GetPolicy(requestType);
        string key = $"rl:{requestType}:{_callerKey}";

        double nowMs = (DateTime.UtcNow - Epoch).TotalMilliseconds;
        double windowStartMs = nowMs - window.TotalMilliseconds;

        try
        {
            // 1. Evict everything that has slid out of the window.
            await _inMemoryDb.SortedSetRemoveRangeByScoreAsync(key, double.NegativeInfinity, windowStartMs);

            // 2. Count requests still inside the window.
            long countInWindow = await _inMemoryDb.SortedSetLengthAsync(key);
            if (countInWindow >= limit)
                throw new AppException(GetRateLimitErrorMessage(requestType), HttpStatusCode.TooManyRequests);

            // 3. Record this request (member must be unique so concurrent requests don't collide).
            await _inMemoryDb.SortedSetAddAsync(key, $"{nowMs}:{Guid.NewGuid():N}", nowMs);

            // 4. Let Redis reclaim the key once the whole window has elapsed with no traffic.
            await _inMemoryDb.KeyExpireAsync(key, window + TimeSpan.FromSeconds(1));
        }
        catch (RedisException)
        {
            // Fail open: if the rate-limit store is unavailable, don't take the API down with it.
            return true;
        }

        return true;
    }

    public async Task ResetLimits(AppConstants.ApiRequestAction requestType)
    {
        await _inMemoryDb.KeyDeleteAsync($"rl:{requestType}:{_callerKey}");
    }

    /// <summary>Per-endpoint policy: (max requests, window). This is where "per-endpoint policies" lives.</summary>
    private static (int limit, TimeSpan window) GetPolicy(AppConstants.ApiRequestAction requestType) => requestType switch
    {
        AppConstants.ApiRequestAction.Login      => (5,  TimeSpan.FromMinutes(1)),
        AppConstants.ApiRequestAction.Register   => (5,  TimeSpan.FromMinutes(5)),
        AppConstants.ApiRequestAction.CreatePost => (10, TimeSpan.FromMinutes(1)),
        AppConstants.ApiRequestAction.UpdatePost => (10, TimeSpan.FromMinutes(1)),
        AppConstants.ApiRequestAction.AddComment => (20, TimeSpan.FromMinutes(1)),
        AppConstants.ApiRequestAction.LikePost   => (60, TimeSpan.FromMinutes(1)),
        AppConstants.ApiRequestAction.UnlikePost => (60, TimeSpan.FromMinutes(1)),
        AppConstants.ApiRequestAction.Follow     => (30, TimeSpan.FromMinutes(1)),
        AppConstants.ApiRequestAction.Unfollow   => (30, TimeSpan.FromMinutes(1)),
        _                                        => (30, TimeSpan.FromMinutes(1))
    };

    public string GetRateLimitErrorMessage(AppConstants.ApiRequestAction requestType) => requestType switch
    {
        AppConstants.ApiRequestAction.CreatePost => "Rate limit exceeded for creating posts. Please try again later.",
        AppConstants.ApiRequestAction.Register   => "Rate limit exceeded for registration. Please try again later.",
        AppConstants.ApiRequestAction.Login       => "Too many login attempts. Please try again later.",
        _                                         => "Rate limit exceeded. Please try again later."
    };
}
