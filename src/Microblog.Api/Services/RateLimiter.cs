using System.Security.Claims;

namespace Microblog.Api.Services;

public class RateLimiter : IRateLimiter
{
    private readonly string? _clientIp = string.Empty;
    private readonly IDatabase _inMemoryDb;
    private readonly long _userId;

    public RateLimiter(IConnectionMultiplexer connectionMultiplexer, IHttpContextAccessor httpContextAccessor)
    {
        _inMemoryDb = connectionMultiplexer.GetDatabase();
        var context = httpContextAccessor.HttpContext;
        if (context?.User?.Identity?.IsAuthenticated ?? false)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long uid)) _userId = uid;
        }

        if (_userId == 0)
        {
            _clientIp = Convert.ToString(httpContextAccessor.HttpContext?.Connection.RemoteIpAddress);
            if (string.IsNullOrWhiteSpace(_clientIp))
                throw new Exception("Could not fetch client IP address for rate limiting");
        }
    }

    public async Task<bool> IsRequestAllowedAsync(AppConstants.ApiRequestAction requestType)
    {
        int limit = requestType switch
        {
            AppConstants.ApiRequestAction.CreatePost => 5,
            AppConstants.ApiRequestAction.CreateUser => 5,
            AppConstants.ApiRequestAction.Login => 5,
            AppConstants.ApiRequestAction.UnlikePost => 25,
            AppConstants.ApiRequestAction.LikePost => 25,
            _ => 10
        };
        var period = requestType switch
        {
            AppConstants.ApiRequestAction.CreatePost => TimeSpan.FromMinutes(1),
            AppConstants.ApiRequestAction.CreateUser => TimeSpan.FromMinutes(5),
            AppConstants.ApiRequestAction.Login => TimeSpan.FromMinutes(1),
            AppConstants.ApiRequestAction.LikePost => TimeSpan.FromMinutes(1),
            AppConstants.ApiRequestAction.UnlikePost => TimeSpan.FromMinutes(1),
            _ => TimeSpan.FromMinutes(1)
        };

        if (_userId > 0)
        {
            string key = $"{requestType}:userId:{_userId}";
            long count = await _inMemoryDb.StringIncrementAsync(key);
            if (count == 1) await _inMemoryDb.KeyExpireAsync(key, period);
            if (count > limit)
                throw new AppException(GetRateLimitErrorMessage(requestType), HttpStatusCode.TooManyRequests);
        }
        else
        {
            string key = $"{requestType}:clientIp:{_clientIp}";
            long count = await _inMemoryDb.StringIncrementAsync(key);
            if (count == 1) await _inMemoryDb.KeyExpireAsync(key, period);
            if (count > limit)
                throw new AppException(GetRateLimitErrorMessage(requestType), HttpStatusCode.TooManyRequests);
        }

        return true;
    }

    public async Task ResetLimits(AppConstants.ApiRequestAction requestType)
    {
        string key = $"{requestType}:{_clientIp}";
        await _inMemoryDb.KeyDeleteAsync(key);
    }

    public string GetRateLimitErrorMessage(AppConstants.ApiRequestAction requestType)
    {
        return requestType switch
        {
            AppConstants.ApiRequestAction.CreatePost =>
                "Rate limit exceeded for creating posts. Please try again later.",
            AppConstants.ApiRequestAction.CreateUser =>
                "Rate limit exceeded for creating users. Please try again later.",
            AppConstants.ApiRequestAction.Login => "Rate limit exceeded for login attempts. Please try again later.",
            _ => "Rate limit exceeded. Please try again later."
        };
    }
}