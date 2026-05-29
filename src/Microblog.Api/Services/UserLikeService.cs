namespace Microblog.Api.Services;

public class UserLikeService(
    IPostService postService,
    IUserService userService,
    AppDbContext dbContext,
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<IUserLikeService> logger)
    : IUserLikeService
{
    private readonly IDatabase _inMemoryDb = connectionMultiplexer.GetDatabase();
    private readonly ILogger _logger = logger;

    public async Task LikePostAsync(long userId, long postId, bool useCache = true)
    {
        if (userId <= 0 || postId <= 0) throw new Exception("Cannot like post");

        var userLikeObj = new UserLike
        {
            UserId = userId,
            PostId = postId
        };

        /* Try cache */
        try
        {
            if (!useCache) throw new Exception();

            var op_likeEventForDbSyncQueueAdd =
                AppConstants.InMemoryOperationType.LIKE_EVENT_FOR_BACKGROUND_SYNC_QUEUE_ADD;
            var op_postLikesInc = AppConstants.InMemoryOperationType.POST_LIKES_INCREASED_BY_USER_ID;
            var op_userLikedPost = AppConstants.InMemoryOperationType.USER_RECENTLY_LIKED_POST;

            string key_postLikesInc = InMemoryUtils.GetKey(op_postLikesInc, postId);
            string key_likeEventForDbSyncQueueAdd = InMemoryUtils.GetKey(op_likeEventForDbSyncQueueAdd);
            string key_userRecentLikedPost = InMemoryUtils.GetKey(op_postLikesInc);

            /* First - Clear queue overflow */
            await new InMemoryUtils(_inMemoryDb).FlushAndClearQueueOverflow(op_likeEventForDbSyncQueueAdd);
            await new InMemoryUtils(_inMemoryDb)
                .ClearQueueOverflow(
                    op_userLikedPost); // doesn't flush to db because we keep only the like event record and not per user record in db

            /* Then - Add like with sorting to sort by least recently added for users who have liked this post and Set expire key - for this 12Hrs */
            bool isNewlyAddedLike =
                await _inMemoryDb.SortedSetAddAsync(key_postLikesInc, userId, InMemoryUtils.GetUniqueRank());
            await _inMemoryDb.KeyExpireAsync(key_postLikesInc,
                AppConstants.CacheConfigDict[op_postLikesInc].CacheTTLSeconds);

            bool isNewlyLikedPost =
                await _inMemoryDb.SortedSetAddAsync(key_userRecentLikedPost, postId, InMemoryUtils.GetUniqueRank());
            await _inMemoryDb.KeyExpireAsync(key_userRecentLikedPost,
                AppConstants.CacheConfigDict[op_userLikedPost].CacheTTLSeconds);

            var likeEvent = new LikeEvent
            {
                UserId = userId,
                PostId = postId,
                Action = LikeAction.Like
            };

            await _inMemoryDb.ListRightPushAsync(key_likeEventForDbSyncQueueAdd,
                InMemoryUtils.ConvertToInMemoryValue(likeEvent));
        }
        /* Fallback to DB */
        catch (Exception ex)
        {
            await LikePostDbFallbackAsync(userId, postId);
        }
    }

    public async Task UnlikePostAsync(long userId, long postId, bool useCache = true)
    {
        if (userId <= 0 || postId <= 0) throw new Exception("Cannot unlike post");

        var userLikeObj = new UserLike
        {
            UserId = userId,
            PostId = postId
        };

        /* Try cache */
        try
        {
            if (!useCache) throw new Exception();

            var op_likeEventForDbSyncQueueAdd =
                AppConstants.InMemoryOperationType.LIKE_EVENT_FOR_BACKGROUND_SYNC_QUEUE_ADD;
            var op_postLikesInc = AppConstants.InMemoryOperationType.POST_LIKES_INCREASED_BY_USER_ID;
            var op_userLikedPost = AppConstants.InMemoryOperationType.USER_RECENTLY_LIKED_POST;


            string key_likePost = InMemoryUtils.GetKey(op_postLikesInc, postId);
            string key_likeEventQueueAdd = InMemoryUtils.GetKey(op_likeEventForDbSyncQueueAdd);
            string key_userLikedPost = InMemoryUtils.GetKey(op_userLikedPost);

            bool isRemoved = await _inMemoryDb.SortedSetRemoveAsync(key_likePost, userId);
            await _inMemoryDb.KeyExpireAsync(key_likePost,
                AppConstants.CacheConfigDict[op_likeEventForDbSyncQueueAdd].CacheTTLSeconds);

            isRemoved = await _inMemoryDb.SortedSetRemoveAsync(key_userLikedPost, postId);
            await _inMemoryDb.KeyExpireAsync(key_userLikedPost,
                AppConstants.CacheConfigDict[op_userLikedPost].CacheTTLSeconds);

            var likeEvent = new LikeEvent
            {
                UserId = userId,
                PostId = postId,
                Action = LikeAction.Unlike
            };

            await _inMemoryDb.ListRightPushAsync(key_likeEventQueueAdd,
                InMemoryUtils.ConvertToInMemoryValue(likeEvent));
        }
        /* Fallback to DB */
        catch (Exception ex)
        {
            await UnlikePostDbFallbackAsync(userId, postId);
        }
    }

    public async Task<(long likesCount, bool isLikedByUser)> GetPostLikesAndIsLikedByUserAsync(long userId, long postId,
        bool useCache = true)
    {
        if (userId <= 0 || postId <= 0) throw new Exception("Cannot fetch post likes");

        long likesCount = 0;
        bool isLikedByUser = false;

        try
        {
            if (!useCache) throw new Exception();

            string key_likePost =
                InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.POST_LIKES_INCREASED_BY_USER_ID, postId);
            likesCount = Convert.ToInt64(await _inMemoryDb.SortedSetLengthAsync(key_likePost));
            if (likesCount == 0) throw new Exception();
            isLikedByUser = await _inMemoryDb.SetContainsAsync(key_likePost, userId);

            return (likesCount, isLikedByUser);
        }
        catch (Exception ex)
        {
            (likesCount, isLikedByUser) = await GetPostLikesAndIsLikedByUserFallbackDbAsync(userId, postId);
        }

        return (likesCount, isLikedByUser);
    }

    public async Task<List<long>> GetRecentlyLikedPostIdsByUserAsync(long userId, int page = 1, int pageSize = 10,
        bool useCache = true)
    {
        var recentlyLikedPostIdsList = new List<long>();

        try
        {
            if (!useCache) throw new Exception();

            string key_userLikePost =
                InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.LIKE_EVENT_FOR_BACKGROUND_SYNC_QUEUE_ADD);
            recentlyLikedPostIdsList = InMemoryUtils.GetMembersFromValueListAs<long>(
                await _inMemoryDb.SortedSetRangeByRankAsync(key_userLikePost, (page - 1) * pageSize, pageSize,
                    Order.Descending));
            if (recentlyLikedPostIdsList.Count() < pageSize) throw new Exception();
        }
        catch (Exception ex)
        {
            recentlyLikedPostIdsList = await GetRecentlyLikedPostIdsByUserFallbackToDbAsync(userId, page, pageSize);
        }

        return recentlyLikedPostIdsList;
    }

    public async Task<List<Post>> GetRecentlyLikedPostsByUserAsync(long userId, int page = 1, int limit = 10,
        bool useCache = true)
    {
        var recentlyLikedPosts = new List<Post>();

        var recentlyLikedPostIds = await GetRecentlyLikedPostIdsByUserAsync(userId, page, limit, useCache);
        recentlyLikedPosts = await postService.GetPostsByIdListAsync(recentlyLikedPostIds);

        return recentlyLikedPosts;
    }

    private async Task LikePostDbFallbackAsync(long userId, long postId)
    {
        if (userId <= 0 || postId <= 0) throw new Exception("Cannot like post");

        var userLikeObj = new UserLike
        {
            UserId = userId,
            PostId = postId
        };

        await userService.GetUserByIdAsync(userId);

        var post = await postService.GetPostByIdAsync(postId);
        if (post != null)
        {
            await dbContext.UserLikes.AddAsync(userLikeObj);
            await dbContext.SaveChangesAsync();
        }
        /* TODO:Add Cache post.Tags as recently liked and increase counter for this tag to improve its suggestability */
    }

    private async Task UnlikePostDbFallbackAsync(long userId, long postId)
    {
        if (userId == 0 || postId == 0) throw new Exception("Cannot like post");

        /* Verify user and post exist */
        await userService.GetUserByIdAsync(userId);
        var post = await postService.GetPostByIdAsync(postId);

        var likedPost = await dbContext.UserLikes.FirstOrDefaultAsync(l => l.UserId == userId && l.PostId == postId);
        if (likedPost != null) dbContext.UserLikes.Remove(likedPost);
        await dbContext.SaveChangesAsync();
        /* TODO:Remove Cache post.Tags as recently liked and decrease counter for this tag to improve its suggestability */
    }

    private async Task<(long likesCount, bool isLikedByUser)> GetPostLikesAndIsLikedByUserFallbackDbAsync(long userId,
        long postId, bool isFallback = true)
    {
        if (userId <= 0 || postId <= 0) throw new Exception("Cannot fetch post likes");

        long likesCount = await dbContext.UserLikes.CountAsync(like => like.PostId == postId);
        bool isLikedByUser = await dbContext.UserLikes.Where(like => like.UserId == userId && like.PostId == postId)
            .FirstOrDefaultAsync() != null;

        if (isFallback)
            /* Re-populate redis */
            if (likesCount > 0)
            {
                var op_likePost = AppConstants.InMemoryOperationType.POST_LIKES_INCREASED_BY_USER_ID;
                long cacheMemoryLimit = AppConstants.CacheConfigDict[op_likePost].CacheMemoryLimit;

                var userIds = await dbContext.UserLikes
                    .Where(like => like.PostId == postId)
                    .Select(like => like.UserId).Take((int)cacheMemoryLimit)
                    .ToListAsync();
                if (userIds.Count() > 0)
                {
                    string key_likePost =
                        InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.POST_LIKES_INCREASED_BY_USER_ID,
                            postId);
                    await _inMemoryDb.KeyDeleteAsync(key_likePost);
                    await _inMemoryDb.SetAddAsync(key_likePost, InMemoryUtils.ConvertToInMemoryValue(userIds));
                    await _inMemoryDb.KeyExpireAsync(key_likePost,
                        AppConstants.CacheConfigDict[AppConstants.InMemoryOperationType.POST_LIKES_INCREASED_BY_USER_ID]
                            .CacheTTLSeconds);
                }
            }

        return (likesCount, isLikedByUser);
    }

    private async Task<List<long>> GetRecentlyLikedPostIdsByUserFallbackToDbAsync(long userId, int page = 1,
        int pageSize = 10)
    {
        var op_userLikedPost = AppConstants.InMemoryOperationType.USER_RECENTLY_LIKED_POST;
        string key_userLikedPost = InMemoryUtils.GetKey(op_userLikedPost);

        int inMemoryLimit = AppConstants.CacheConfigDict[op_userLikedPost].CacheMemoryLimit;
        var recentlyLikedPostsFromDb = await dbContext.UserLikes
            .Where(u => u.UserId == userId)
            .OrderByDescending(u => u.CreatedAt)
            .Take(inMemoryLimit)
            .ToListAsync();

        /* Refresh cache concurrently */
        _ = Task.Run(async () =>
        {
            try
            {
                await _inMemoryDb.KeyDeleteAsync(key_userLikedPost);

                var tasks = recentlyLikedPostsFromDb.Select(u =>
                    _inMemoryDb.SortedSetAddAsync(key_userLikedPost, u.PostId,
                        InMemoryUtils.GetUniqueRank(u.CreatedAt)));

                await Task.WhenAll(tasks);

                await _inMemoryDb.KeyExpireAsync(key_userLikedPost,
                    AppConstants.CacheConfigDict[op_userLikedPost].CacheTTLSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to refresh user recently liked posts cache");
            }
        });

        return recentlyLikedPostsFromDb
            .Select(u => u.PostId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }
}