using Microblog.LoadTests.Shared;
using Microblog.LoadTests.Scenarios;
using Microblog.LoadTests.Scenarios.Likes;
using NBomber.Contracts;
using NBomber.CSharp;

// ── The dispatcher ──────────────────────────────────────────────────────────────
// This is the menu. You pick a scenario by name; it runs just that one.
// Each scenario sets up everything it needs on its own, so they're independent.
//
//   dotnet run -- --scenario smoke        run one scenario
//   dotnet run -- --scenario all          run every scenario, one after another
//   dotnet run -- --list                  show available scenarios

// All known scenarios: name -> how to build it.
var registry = new Dictionary<string, Func<ScenarioProps>>(StringComparer.OrdinalIgnoreCase)
{
    [Smoke.Name]            = Smoke.Create,
    [LikeStormHotPost.Name] = LikeStormHotPost.Create,
    [LikeReadCounts.Name]   = LikeReadCounts.Create,
};

// What did the user ask for? (--scenario X, or the SCENARIO env var)
string? requested = GetArg(args, "--scenario") ?? Environment.GetEnvironmentVariable("SCENARIO");

if (args.Contains("--list") || requested is null)
{
    Console.WriteLine("Available scenarios:");
    foreach (var name in registry.Keys) Console.WriteLine($"  - {name}");
    Console.WriteLine("  - all");
    Console.WriteLine("\nRun with:  dotnet run -- --scenario <name>");
    if (requested is null) return;
}

Console.WriteLine($"Target API   : {Config.BaseUrl}");
Console.WriteLine($"Load profile : {Config.LoadProfile}");

// Build the chosen scenario(s).
ScenarioProps[] toRun =
    requested.Equals("all", StringComparison.OrdinalIgnoreCase)
        ? registry.Values.Select(build => build()).ToArray()
        : registry.TryGetValue(requested, out var build)
            ? [build()]
            : throw new Exception($"Unknown scenario '{requested}'. Run with --list to see options.");

NBomberRunner
    .RegisterScenarios(toRun)
    .Run();

return;

// Reads the value after a flag, e.g. GetArg(args, "--scenario") for "--scenario smoke".
static string? GetArg(string[] args, string flag)
{
    int i = Array.IndexOf(args, flag);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}
