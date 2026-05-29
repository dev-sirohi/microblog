namespace Microblog.Api.Filters.Attributes;

public class RateLimitAttribute : TypeFilterAttribute
{
    // TypeFilterAttribute is used when you need DI injected services in Filter,
    // so this creates the target filter in a way that allows DI injections in class.
    public RateLimitAttribute(AppConstants.ApiRequestAction action) : base(typeof(RateLimitFilter))
    {
        // Passes arguments to the type-filter constructor
        Arguments = new object[] { action };
    }
}