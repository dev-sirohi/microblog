using NBomber.Contracts;
using NBomber.CSharp;

namespace Microblog.LoadTests.Shared;

// "How hard to push." Every scenario uses LoadProfiles.Selected(), which reads
// LOAD_PROFILE from the .env file. Same test, different amount of pressure.
//
//   rate = requests started per second.
public static class LoadProfiles
{
    public static LoadSimulation[] Selected()
    {
        int dur = Config.DurationSeconds;
        var during = TimeSpan.FromSeconds(dur);
        var oneSec = TimeSpan.FromSeconds(1);

        return Config.LoadProfile.ToLowerInvariant() switch
        {
            // Gentle sanity check — run this first.
            "smoke" =>
            [
                Simulation.Inject(rate: 5, interval: oneSec, during: TimeSpan.FromSeconds(10))
            ],

            // Slowly turn the pressure up to find where things slow down.
            "ramp" =>
            [
                Simulation.RampingInject(rate: 200, interval: oneSec, during: during)
            ],

            // Calm, then a sudden flood, then calm again.
            "spike" =>
            [
                Simulation.Inject(rate: 20,  interval: oneSec, during: TimeSpan.FromSeconds(dur / 3)),
                Simulation.Inject(rate: 500, interval: oneSec, during: TimeSpan.FromSeconds(dur / 3)),
                Simulation.Inject(rate: 20,  interval: oneSec, during: TimeSpan.FromSeconds(dur / 3))
            ],

            // Steady pressure for a long time (catches slow leaks / queue drift).
            "soak" =>
            [
                Simulation.Inject(rate: 100, interval: oneSec, during: during)
            ],

            // Keep pushing harder and harder until it breaks.
            "stress" =>
            [
                Simulation.RampingInject(rate: 2000, interval: oneSec, during: during)
            ],

            // Anything unrecognised → a steady, moderate load.
            _ =>
            [
                Simulation.Inject(rate: 100, interval: oneSec, during: during)
            ]
        };
    }
}
