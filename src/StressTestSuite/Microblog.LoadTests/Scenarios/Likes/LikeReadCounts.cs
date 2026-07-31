using NBomber.Contracts;
using NBomber.CSharp;
using Microblog.LoadTests.Shared;

namespace Microblog.LoadTests.Scenarios.Likes;

// Reads a post's like count and whether the current user liked it.
// Tests the READ side of likes: served from Redis, falling back to SQL on a miss.
//
// Endpoint: GET /api/UserLike/{postId}   (added to the API for load testing)
public static class LikeReadCounts
{
    public const string Name = "like-read";

    public static ScenarioProps Create()
    {
        UserPool pool = null!;
        long postId = 0;

        return Scenario.Create(Name, async _ =>
            {
                var user = pool.Next();
                var resp = await user.Client.GetAsync($"/api/UserLike/{postId}");
                return resp.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
            })
            .WithInit(async _ =>
            {
                pool = await UserPool.CreateAsync(Config.SeedUsers);

                // Create a post and have some users like it, so there's a real count to read.
                postId = await pool.First().CreatePostAsync("A popular post");
                int likers = Math.Min(Config.SeedUsers, 20);
                for (int i = 0; i < likers; i++)
                    await pool.Next().Client.PostAsync($"/api/UserLike/like/{postId}", null);

                Console.WriteLine($"[like-read] reading likes for post {postId} ({likers} likers seeded)");
            })
            .WithoutWarmUp()
            .WithLoadSimulations(LoadProfiles.Selected());
    }
}
