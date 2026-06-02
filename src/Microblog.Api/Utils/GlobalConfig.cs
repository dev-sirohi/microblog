namespace Microblog.Api.Utils;

public sealed class GlobalConfig
{
    public bool DisableRateLimiting { get; private set; }
    
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
    }
}