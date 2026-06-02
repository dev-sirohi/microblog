namespace Microblog.Api.Utils;

public class GlobalConfig
{
    private IConfiguration _configuration;
    private bool _isInitialized = false;
    public bool DisableRateLimiting { get; private set; }

    public GlobalConfig(IConfiguration configuration) {
        if (_isInitialized)
        {
            throw new Exception("Cannot reinitialize global config");
        }
        _isInitialized = true;
        _configuration = configuration;
        Initialize();
    }

    private void Initialize()
    {
        DisableRateLimiting = _configuration.GetValue();
    }
}