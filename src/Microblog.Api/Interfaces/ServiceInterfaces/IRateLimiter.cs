namespace Microblog.Api.Interfaces.ServiceInterfaces
{
    public interface IRateLimiter
    {
        Task<bool> IsRequestAllowedAsync(AppConstants.ApiRequestAction requestType);
        Task ResetLimits(AppConstants.ApiRequestAction requestType);
        string GetRateLimitErrorMessage(AppConstants.ApiRequestAction requestType);
    }
}
