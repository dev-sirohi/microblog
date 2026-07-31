using Microblog.Api.Services.BackgroundProcesses;

namespace Microblog.Api.Services;

public class UserFollowService(
    AppDbContext dbContext,
    IConnectionMultiplexer connectionMultiplexer,
    UserService userService)
{
    private readonly IDatabase _redis = connectionMultiplexer.GetDatabase();

    public async Task FollowUserAsync(long followerId, long followingId)
    {
        if (followerId <= 0 || followingId <= 0) throw new AppException("Cannot follow user", HttpStatusCode.BadRequest);
        if (followerId == followingId) throw new AppException("Cannot follow yourself", HttpStatusCode.BadRequest);

        await userService.GetUserByIdAsync(followerId);
        await userService.GetUserByIdAsync(followingId);

        await _redis.SetAddAsync(SyncQueue.UserFollowingKey(followerId), followingId);
        await _redis.SetAddAsync(SyncQueue.UserFollowersKey(followingId), followerId);

        await EnqueueAsync(new FollowEvent { FollowerId = followerId, FollowingId = followingId, Action = FollowAction.Follow });
    }

    public async Task UnfollowUserAsync(long followerId, long followingId)
    {
        if (followerId <= 0 || followingId <= 0) throw new AppException("Cannot unfollow user", HttpStatusCode.BadRequest);
        if (followerId == followingId) throw new AppException("Cannot unfollow yourself", HttpStatusCode.BadRequest);

        await _redis.SetRemoveAsync(SyncQueue.UserFollowingKey(followerId), followingId);
        await _redis.SetRemoveAsync(SyncQueue.UserFollowersKey(followingId), followerId);

        await EnqueueAsync(new FollowEvent { FollowerId = followerId, FollowingId = followingId, Action = FollowAction.Unfollow });
    }

    public async Task<IReadOnlyCollection<long>> GetFollowingIdListAsync(long userId)
    {
        string key = SyncQueue.UserFollowingKey(userId);
        var members = await _redis.SetMembersAsync(key);
        if (members.Length > 0)
            return members.Select(m => (long)m).ToList();

        var fromDb = await dbContext.UserFollows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToListAsync();

        if (fromDb.Count > 0)
            await _redis.SetAddAsync(key, fromDb.Select(id => (RedisValue)id).ToArray());

        return fromDb;
    }

    public async Task<IReadOnlyCollection<long>> GetFollowerIdListAsync(long userId)
    {
        string key = SyncQueue.UserFollowersKey(userId);
        var members = await _redis.SetMembersAsync(key);
        if (members.Length > 0)
            return members.Select(m => (long)m).ToList();

        var fromDb = await dbContext.UserFollows
            .Where(f => f.FollowingId == userId)
            .Select(f => f.FollowerId)
            .ToListAsync();

        if (fromDb.Count > 0)
            await _redis.SetAddAsync(key, fromDb.Select(id => (RedisValue)id).ToArray());

        return fromDb;
    }

    private Task EnqueueAsync(FollowEvent evt) =>
        _redis.SortedSetAddAsync(SyncQueue.FollowEventsKey, SyncQueue.Serialize(evt), SyncQueue.NowScore());
}
