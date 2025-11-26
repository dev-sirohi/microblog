namespace Microblog.Api.Services
{
    public class UserFollowService : IUserFollowService
    {
        private readonly AppDbContext _dbContext;
        private readonly IDatabase _inMemoryDb;
        private readonly IUserService _userService;

        public UserFollowService(AppDbContext dbContext, IConnectionMultiplexer connectionMultiplexer, IUserService userService)
        {
            _dbContext = dbContext;
            _inMemoryDb = connectionMultiplexer.GetDatabase();
            _userService = userService;
        }

        public async Task FollowUserAsync(long followerId, long followingId)
        {
            if (followerId == 0 || followingId == 0)
            {
                throw new Exception("Cannot follow user");
            }
            if (followerId == followingId)
            {
                throw new Exception("Cannot follow yourself");
            }

            /* Verify users exist */
            User follower = await _userService.GetUserByIdAsync(followerId);
            User following = await _userService.GetUserByIdAsync(followingId);

            UserFollow userFollow = new UserFollow
            {
                FollowerId = followerId,
                FollowingId = followingId
            };

            await _dbContext.UserFollows.AddAsync(userFollow);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw new Exception("Cannot follow user");
            }
            
            await _inMemoryDb.SetAddAsync(InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWING, followerId), followingId);
            await _inMemoryDb.SetAddAsync(InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWING, followingId), followerId);
            await _inMemoryDb.StringIncrementAsync(InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWING_COUNT));
            await _inMemoryDb.StringIncrementAsync(InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWING_COUNT));
        }

        public async Task UnfollowUserAsync(long followerId, long followingId)
        {
            if (followerId == 0 || followingId == 0)
            {
                throw new Exception("Cannot follow user");
            }
            if (followerId == followingId)
            {
                throw new Exception("Cannot follow yourself");
            }

            /* Verify users exist */
            User follower = await _userService.GetUserByIdAsync(followerId);
            User following = await _userService.GetUserByIdAsync(followingId);

            UserFollow userFollow = new UserFollow
            {
                FollowerId = followerId,
                FollowingId = followingId
            };

            UserFollow? relationObj = await _dbContext.UserFollows.FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

            if (relationObj != null)
            {
                _dbContext.UserFollows.Remove(relationObj);
            }
            await _dbContext.SaveChangesAsync();

            await _inMemoryDb.SetRemoveAsync(InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWING, followerId), followingId);
            await _inMemoryDb.SetRemoveAsync(InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWING, followingId), followerId);
            await _inMemoryDb.StringDecrementAsync(InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWING_COUNT));
            await _inMemoryDb.StringDecrementAsync(InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWING_COUNT));
        }

        public async Task<IReadOnlyCollection<long>> GetFollowingIdListAsync(long userId)
        {
            string key = InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWING, userId);
            List<long> followingIdList = await new InMemoryUtils(_inMemoryDb).GetSetMembersAsync<long>(key);

            if (followingIdList.Count == 0)
            {
                /* Cache fail -> fallback to DB */
                followingIdList = await _dbContext.UserFollows
                    .Where(f => f.FollowerId == userId)
                    .Select(f => f.FollowingId)
                    .ToListAsync();

                if (followingIdList.Count > 0)
                {
                    await _inMemoryDb.SetAddAsync(key, followingIdList.Select(i => InMemoryUtils.ConvertToInMemoryValue(i)).ToArray());
                }
            }

            return followingIdList;
        }

        public async Task<IReadOnlyCollection<long>> GetFollowerIdListAsync(long userId)
        {
            string key = InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWED_BY, userId);
            List<long> followerIdList = await new InMemoryUtils(_inMemoryDb).GetSetMembersAsync<long>(key);

            if (followerIdList.Count == 0)
            {
                /* Cache fail -> fallback to DB */
                followerIdList = await _dbContext.UserFollows
                    .Where(f => f.FollowerId == userId)
                    .Select(f => f.FollowingId)
                    .ToListAsync();

                if (followerIdList.Count > 0)
                {
                    await _inMemoryDb.SetAddAsync(key, followerIdList.Select(i => InMemoryUtils.ConvertToInMemoryValue(i)).ToArray());
                }
            }

            return followerIdList;
        }
    }
}
