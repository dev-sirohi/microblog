using System.Security.Claims;

namespace Microblog.Api.Services;

public interface IRateLimiter
{
    Task<bool> IsRequestAllowedAsync(AppConstants.ApiRequestAction requestType);
    Task ResetLimits(AppConstants.ApiRequestAction requestType);
    string GetRateLimitErrorMessage(AppConstants.ApiRequestAction requestType);
}

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
            await _inMemoryDb.SortedSetRemoveRangeByScoreAsync(key, double.NegativeInfinity, windowStartMs);

            long countInWindow = await _inMemoryDb.SortedSetLengthAsync(key);
            if (countInWindow >= limit)
                throw new AppException(GetRateLimitErrorMessage(requestType), HttpStatusCode.TooManyRequests);

            await _inMemoryDb.SortedSetAddAsync(key, $"{nowMs}:{Guid.NewGuid():N}", nowMs);

            await _inMemoryDb.KeyExpireAsync(key, window + TimeSpan.FromSeconds(1));
        }
        catch (RedisException)
        {
            return true;
        }

        return true;
    }

    public async Task ResetLimits(AppConstants.ApiRequestAction requestType)
    {
        await _inMemoryDb.KeyDeleteAsync($"rl:{requestType}:{_callerKey}");
    }

    private static (int limit, TimeSpan window) GetPolicy(AppConstants.ApiRequestAction requestType) => requestType switch
    {
        AppConstants.ApiRequestAction.Login      => (5,  TimeSpan.FromMinutes(1)),
        AppConstants.ApiRequestAction.Register   => (5,  TimeSpan.FromMinutes(5)),
        AppConstants.ApiRequestAction.CreatePost => (10, TimeSpan.FromMinutes(1)),
        AppConstants.ApiRequestAction.UpdatePost => (10, TimeSpan.FromMinutes(1)),
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
        AppConstants.ApiRequestAction.Login      => "Too many login attempts. Please try again later.",
        _                                        => "Rate limit exceeded. Please try again later."
    };
}
