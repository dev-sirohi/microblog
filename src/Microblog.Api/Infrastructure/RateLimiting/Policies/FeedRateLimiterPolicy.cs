using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Microblog.Api.Infrastructure.RateLimiting.Policies;

/// <summary>Feed / read endpoints: 60 requests per minute (lenient).</summary>
public sealed class FeedRateLimiterPolicy(IConnectionMultiplexer mux) : IRateLimiterPolicy<string>
{
    public const string Name = "feed";
    private readonly IDatabase _db = mux.GetDatabase();

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected =>
        static (ctx, _) =>
        {
            ctx.HttpContext.Response.Headers.RetryAfter = "60";
            return ValueTask.CompletedTask;
        };

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        string key = ResolveKey(httpContext);
        return RateLimitPartition.Get(key,
            k => new RedisFixedWindowRateLimiter(_db, $"rl:feed:{k}", permitLimit: 60, window: TimeSpan.FromMinutes(1)));
    }

    private static string ResolveKey(HttpContext ctx)
    {
        var userId = ctx.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return !string.IsNullOrEmpty(userId)
            ? $"user:{userId}"
            : ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
