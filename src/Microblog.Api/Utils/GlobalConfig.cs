namespace Microblog.Api.Utils;

public sealed class GlobalConfig(IConfiguration configuration)
{
    public bool DisableRateLimiting { get; } = configuration.GetValue<bool>("RateLimiting:Disabled");

    public string MessagingProvider { get; } = configuration["Features:MessagingProvider"] ?? "none";
}
