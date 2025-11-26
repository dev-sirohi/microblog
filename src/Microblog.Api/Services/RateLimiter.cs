using System.Security.Claims;

namespace Microblog.Api.Services
{
    public class RateLimiter : IRateLimiter
    {
        private readonly IDatabase _inMemoryDb;
        private readonly string? _clientIp = string.Empty;
        private readonly long _userId = 0;

        public RateLimiter(IConnectionMultiplexer connectionMultiplexer, IHttpContextAccessor httpContextAccessor)
        {
            _inMemoryDb = connectionMultiplexer.GetDatabase();
            _clientIp = Convert.ToString(httpContextAccessor.HttpContext?.Connection.RemoteIpAddress);
            if (string.IsNullOrWhiteSpace(_clientIp))
            {
                throw new Exception("Could not fetch client IP address for rate limiting");
            }
            var context = httpContextAccessor.HttpContext;
            if (context?.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long uid))
                {
                    _userId = uid;
                }
            }
        }

        public async Task<bool> IsRequestAllowedAsync(AppConstants.ApiRequestAction requestType)
        {
            bool result = false;

            int limit = requestType switch
            {
                AppConstants.ApiRequestAction.CreatePost => 5,
                AppConstants.ApiRequestAction.CreateUser => 5,
                AppConstants.ApiRequestAction.Login => 5,
                AppConstants.ApiRequestAction.UnlikePost => 25,
                AppConstants.ApiRequestAction.LikePost => 25,
                _ => 10
            };
            TimeSpan period = requestType switch
            {
                AppConstants.ApiRequestAction.CreatePost => TimeSpan.FromMinutes(1),
                AppConstants.ApiRequestAction.CreateUser => TimeSpan.FromMinutes(5),
                AppConstants.ApiRequestAction.Login => TimeSpan.FromMinutes(1),
                AppConstants.ApiRequestAction.LikePost => TimeSpan.FromMinutes(1),
                AppConstants.ApiRequestAction.UnlikePost => TimeSpan.FromMinutes(1),
                _ => TimeSpan.FromMinutes(1)
            };

            string key = $"{requestType}:clientIp:{_clientIp}";
            var currentCount = await _inMemoryDb.StringGetAsync(key);
            if (currentCount.IsNull)
            {
                await _inMemoryDb.StringSetAsync(key, 1, period);
                result = true;
            }

            int count = Convert.ToInt32(currentCount);
            if (count < limit)
            {
                await _inMemoryDb.StringSetAsync(key, ++count);
                result = true;
            }

            long userId = _userId;
            if (result && userId > 0)
            {
                result = false;
                key = $"{requestType}:userId:{userId}";
                currentCount = await _inMemoryDb.StringGetAsync(key);
                if (currentCount.IsNull)
                {
                    await _inMemoryDb.StringSetAsync(key, 1, period);
                    result = true;
                }
                count = Convert.ToInt32(currentCount);
                if (count < limit)
                {
                    await _inMemoryDb.StringSetAsync(key, ++count);
                    result = true;
                }
            }

            if (!result)
            {
                throw new AppException("Too many requests", HttpStatusCode.TooManyRequests);
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
                AppConstants.ApiRequestAction.CreatePost => "Rate limit exceeded for creating posts. Please try again later.",
                AppConstants.ApiRequestAction.CreateUser => "Rate limit exceeded for creating users. Please try again later.",
                AppConstants.ApiRequestAction.Login => "Rate limit exceeded for login attempts. Please try again later.",
                _ => "Rate limit exceeded. Please try again later."
            };
        }
    }
}
