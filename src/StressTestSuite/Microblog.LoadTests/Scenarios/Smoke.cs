using NBomber.Contracts;
using NBomber.CSharp;
using Microblog.LoadTests.Shared;

namespace Microblog.LoadTests.Scenarios;

// The simplest possible test: call the /health endpoint a few times.
// It proves the suite is wired up correctly and can reach your API.
// No login needed. Always run this one first.
public static class Smoke
{
    public const string Name = "smoke";

    public static ScenarioProps Create()
    {
        var client = new HttpClient { BaseAddress = new Uri(Config.BaseUrl) };

        return Scenario.Create(Name, async _ =>
            {
                var resp = await client.GetAsync("/health");
                return resp.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(rate: 5, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)));
    }
}
