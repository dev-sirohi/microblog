namespace Microblog.Api.Services;

public class UserFollowService(
    AppDbContext dbContext,
    IConnectionMultiplexer connectionMultiplexer,
    IUserService userService)
    : IUserFollowService
{
    private readonly IDatabase _inMemoryDb = connectionMultiplexer.GetDatabase();

    public async Task FollowUserAsync(long followerId, long followingId)
    {
        if (followerId == 0 || followingId == 0) throw new Exception("Cannot follow user");
        if (followerId == followingId) throw new Exception("Cannot follow yourself");

        /* Verify users exist */
        var follower = await userService.GetUserByIdAsync(followerId);
        var following = await userService.GetUserByIdAsync(followingId);

        var userFollow = new UserFollow
        {
            FollowerId = followerId,
            FollowingId = followingId
        };

        await dbContext.UserFollows.AddAsync(userFollow);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception)
        {
            throw new Exception("Cannot follow user");
        }

        await _inMemoryDb.SetAddAsync(
            InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWING, followerId), followingId);
        await _inMemoryDb.SetAddAsync(
            InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWING, followingId), followerId);
        await _inMemoryDb.StringIncrementAsync(
            InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWING_COUNT));
        await _inMemoryDb.StringIncrementAsync(
            InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWING_COUNT));
    }

    public async Task UnfollowUserAsync(long followerId, long followingId)
    {
        if (followerId == 0 || followingId == 0) throw new Exception("Cannot follow user");
        if (followerId == followingId) throw new Exception("Cannot follow yourself");

        /* Verify users exist */
        var follower = await userService.GetUserByIdAsync(followerId);
        var following = await userService.GetUserByIdAsync(followingId);

        var userFollow = new UserFollow
        {
            FollowerId = followerId,
            FollowingId = followingId
        };

        var relationObj =
            await dbContext.UserFollows.FirstOrDefaultAsync(f =>
                f.FollowerId == followerId && f.FollowingId == followingId);

        if (relationObj != null) dbContext.UserFollows.Remove(relationObj);
        await dbContext.SaveChangesAsync();

        await _inMemoryDb.SetRemoveAsync(
            InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWING, followerId), followingId);
        await _inMemoryDb.SetRemoveAsync(
            InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWING, followingId), followerId);
        await _inMemoryDb.StringDecrementAsync(
            InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWING_COUNT));
        await _inMemoryDb.StringDecrementAsync(
            InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWING_COUNT));
    }

    public async Task<IReadOnlyCollection<long>> GetFollowingIdListAsync(long userId)
    {
        string key = InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWING, userId);
        var followingIdList = await new InMemoryUtils(_inMemoryDb).GetSetMembersAsync<long>(key);

        if (followingIdList.Count == 0)
        {
            /* Cache fail -> fallback to DB */
            followingIdList = await dbContext.UserFollows
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowingId)
                .ToListAsync();

            if (followingIdList.Count > 0)
                await _inMemoryDb.SetAddAsync(key,
                    followingIdList.Select(i => InMemoryUtils.ConvertToInMemoryValue(i)).ToArray());
        }

        return followingIdList;
    }

    public async Task<IReadOnlyCollection<long>> GetFollowerIdListAsync(long userId)
    {
        string key = InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWED_BY, userId);
        var followerIdList = await new InMemoryUtils(_inMemoryDb).GetSetMembersAsync<long>(key);

        if (followerIdList.Count == 0)
        {
            /* Cache fail -> fallback to DB */
            followerIdList = await dbContext.UserFollows
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowingId)
                .ToListAsync();

            if (followerIdList.Count > 0)
                await _inMemoryDb.SetAddAsync(key,
                    followerIdList.Select(i => InMemoryUtils.ConvertToInMemoryValue(i)).ToArray());
        }

        return followerIdList;
    }
}