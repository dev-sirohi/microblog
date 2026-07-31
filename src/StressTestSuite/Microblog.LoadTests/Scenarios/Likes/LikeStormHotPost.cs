using NBomber.Contracts;
using NBomber.CSharp;
using Microblog.LoadTests.Shared;

namespace Microblog.LoadTests.Scenarios.Likes;

// ===== THE HEADLINE TEST =====
// Many users like the SAME post at once — like a tweet going viral.
//
// Your API is designed to absorb this: a like is written to Redis instantly and
// copied to SQL Server later by a background worker. So likes should stay FAST even
// under heavy load. While this runs, watch in Grafana:
//   - like latency  -> should stay low and flat
//   - microblog_background_sync_queue_depth -> should drain, not grow forever
//
// Endpoint: POST /api/UserLike/like/{postId}
public static class LikeStormHotPost
{
    public const string Name = "like-storm";

    public static ScenarioProps Create()
    {
        // Filled in during Init (which runs once, before the load starts).
        UserPool pool = null!;
        long hotPostId = 0;

        return Scenario.Create(Name, async _ =>
            {
                // A different user likes the one hot post on each iteration.
                var user = pool.Next();
                var resp = await user.Client.PostAsync($"/api/UserLike/like/{hotPostId}", null);
                return resp.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
            })
            .WithInit(async _ =>
            {
                Console.WriteLine($"[like-storm] creating {Config.SeedUsers} logged-in users...");
                pool = await UserPool.CreateAsync(Config.SeedUsers);

                // One user creates the single post that everyone will pile onto.
                hotPostId = await pool.First().CreatePostAsync("This post is going viral");
                Console.WriteLine($"[like-storm] hot post id = {hotPostId}");
            })
            .WithoutWarmUp()
            .WithLoadSimulations(LoadProfiles.Selected());
    }
}
