namespace Microblog.Api.Utils;

public sealed class GlobalConfig
{
    public bool DisableRateLimiting { get; private set; }
    public bool EnableEmbeddings { get; private set; }
    public bool EnableAzureStorage { get; private set; }
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
        if (_environment.IsDevelopment())
        {
            DisableRateLimiting = true;
        }

        EnableEmbeddings = _configuration.GetValue<bool>("Features:EnableEmbeddings");
        EnableAzureStorage = _configuration.GetValue<bool>("Features:EnableAzureStorage");
        MessagingProvider = _configuration["Features:MessagingProvider"] ?? "none";
    }
}
