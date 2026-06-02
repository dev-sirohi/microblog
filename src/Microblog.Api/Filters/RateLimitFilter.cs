using Microsoft.AspNetCore.Mvc.Filters;

namespace Microblog.Api.Filters;

public class RateLimitFilter(AppConstants.ApiRequestAction action,
    IRateLimiter rateLimiterService, GlobalConfig globalConfig) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!globalConfig.DisableRateLimiting)
        {
            await rateLimiterService.IsRequestAllowedAsync(action);
        }
        await next();
    }
}
