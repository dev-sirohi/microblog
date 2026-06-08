using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Microblog.Api.Infrastructure.RateLimiting.Policies;

/// <summary>Auth endpoints: 5 requests per minute (strict — prevents brute-force).</summary>
public sealed class AuthRateLimiterPolicy(IConnectionMultiplexer mux) : IRateLimiterPolicy<string>
{
    public const string Name = "auth";
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
            k => new RedisFixedWindowRateLimiter(_db, $"rl:auth:{k}", permitLimit: 5, window: TimeSpan.FromMinutes(1)));
    }

    private static string ResolveKey(HttpContext ctx)
    {
        var userId = ctx.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return !string.IsNullOrEmpty(userId)
            ? $"user:{userId}"
            : ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
