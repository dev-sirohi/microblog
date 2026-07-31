using System.Net.Http.Json;
using Microblog.Api.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microblog.Tests;

[Collection("api")]
public class BatchedWritePathTests(MicroblogApiFactory factory)
{
    [Fact]
    public async Task Like_Is_Served_From_Cache_Immediately_And_Drained_To_Sql_By_Background_Worker()
    {
        var (client, _) = await TestHelpers.RegisterAndLoginAsync(factory);

        var create = await client.PostAsJsonAsync("/api/post", new { Content = "hello from the batched write path" });
        create.EnsureSuccessStatusCode();
        long postId = (await TestHelpers.ReadDataAsync(create)).GetProperty("Id").GetInt64();

        (await client.PostAsync($"/api/userlike/like/{postId}", null)).EnsureSuccessStatusCode();

        var likesRes = await client.GetAsync($"/api/userlike/{postId}");
        likesRes.EnsureSuccessStatusCode();
        var likesData = await TestHelpers.ReadDataAsync(likesRes);
        Assert.Equal(1, likesData.GetProperty("LikesCount").GetInt64());
        Assert.True(likesData.GetProperty("IsLikedByUser").GetBoolean());

        Assert.True(await EventuallyAsync(async db =>
            await db.UserLikes.AnyAsync(l => l.PostId == postId)));
    }

    [Fact]
    public async Task Follow_Is_Queued_And_Drained_To_Sql_By_Background_Worker()
    {
        var (follower, _) = await TestHelpers.RegisterAndLoginAsync(factory);
        var (_, followeeName) = await TestHelpers.RegisterAndLoginAsync(factory);

        long followeeId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            followeeId = await db.Users.Where(u => u.Username == followeeName).Select(u => u.Id).FirstAsync();
        }

        (await follower.PostAsync($"/api/userfollow/follow/{followeeId}", null)).EnsureSuccessStatusCode();

        Assert.True(await EventuallyAsync(async db =>
            await db.UserFollows.AnyAsync(f => f.FollowingId == followeeId)));
    }

    private async Task<bool> EventuallyAsync(Func<AppDbContext, Task<bool>> predicate, int timeoutSeconds = 20)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                if (await predicate(db)) return true;
            }
            await Task.Delay(500);
        }
        return false;
    }
}
