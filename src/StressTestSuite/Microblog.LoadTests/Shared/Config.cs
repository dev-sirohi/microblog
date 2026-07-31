namespace Microblog.LoadTests.Shared;

// All settings come from the .env file (loaded once here).
// Change the .env file, not the code, to retarget or retune the suite.
public static class Config
{
    // Runs the .env loader the first time any setting is read.
    static Config() => DotEnv.Load();

    // The only required setting: where your API lives.
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("BASE_URL")
        ?? throw new Exception("BASE_URL is not set. Copy .env.example to .env and set BASE_URL.");

    // How hard to push: smoke | ramp | spike | soak | stress  (see LoadProfiles.cs)
    public static string LoadProfile => Get("LOAD_PROFILE", "ramp");

    // How long the load runs, in seconds.
    public static int DurationSeconds => GetInt("DURATION_SECONDS", 60);

    // How many fake logged-in users each scenario creates before it starts.
    public static int SeedUsers => GetInt("SEED_USERS", 50);

    private static string Get(string key, string fallback) =>
        Environment.GetEnvironmentVariable(key) is { Length: > 0 } v ? v : fallback;

    private static int GetInt(string key, int fallback) =>
        int.TryParse(Get(key, fallback.ToString()), out var n) ? n : fallback;
}
