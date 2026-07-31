namespace Microblog.Api.Utils;

public sealed class GlobalConfig
{
    public bool DisableRateLimiting { get; private set; }
    public bool EnableEmbeddings { get; private set; }
    public string MessagingProvider { get; private set; } = "none";

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public GlobalConfig(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
        Initialize();
    }

    private void Initialize()
    {
        // Rate limiting is off in Development by default, but an explicit config flag
        // ("RateLimiting:Disabled") always wins — this lets integration tests exercise
        // the limiter while still running under the Development environment.
        DisableRateLimiting = _configuration.GetValue<bool?>("RateLimiting:Disabled")
                              ?? _environment.IsDevelopment();

        EnableEmbeddings = _configuration.GetValue<bool>("Features:EnableEmbeddings");
        MessagingProvider = _configuration["Features:MessagingProvider"] ?? "none";
    }
}
