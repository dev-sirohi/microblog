namespace Microblog.Api.Filters.Attributes;

public class RateLimitAttribute : TypeFilterAttribute
{
    public RateLimitAttribute(AppConstants.ApiRequestAction action) : base(typeof(RateLimitFilter))
    {
        Arguments = new object[] { action };
    }
}
