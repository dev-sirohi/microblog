using Microblog.Api.Infrastructure.Observability;
using Microblog.Api.Services.BackgroundProcesses;

namespace Microblog.Api.Services;

/// <summary>
/// Like/unlike writes take the fast path: they update the Redis read cache and drop an
/// event onto a Redis queue, then return immediately. <see cref="BackgroundProcesses.BackgroundSyncService"/>
/// drains that queue into SQL Server in batches. Reads are served from Redis and fall back
/// to SQL (rebuilding the cache) on a miss.
/// </summary>
public class UserLikeService(
    IPostService postService,
    AppDbContext dbContext,
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<IUserLikeService> logger)
    : IUserLikeService
{
    private readonly IDatabase _redis = connectionMultiplexer.GetDatabase();
    private readonly ILogger _logger = logger;

    public async Task LikePostAsync(long userId, long postId, bool useCache = true)
    {
        if (userId <= 0 || postId <= 0) throw new Exception("Cannot like post");

        if (!useCache)
        {
            await ApplyLikeToDbAsync(userId, postId);
            return;
        }

        try
        {
            // 1. Update the read cache so GET reflects the like instantly.
            await _redis.SortedSetAddAsync(SyncQueue.PostLikersKey(postId), userId, SyncQueue.NowScore());

            // 2. Queue the event for the background drain to persist to SQL.
            await EnqueueAsync(new LikeEvent { UserId = userId, PostId = postId, Action = LikeAction.Like });

            AppMetrics.PostLikes.Inc();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Like fast-path failed for user {UserId} post {PostId}; writing straight to SQL", userId, postId);
            await ApplyLikeToDbAsync(userId, postId);
        }
    }

    public async Task UnlikePostAsync(long userId, long postId, bool useCache = true)
    {
        if (userId <= 0 || postId <= 0) throw new Exception("Cannot unlike post");

        if (!useCache)
        {
            await ApplyUnlikeToDbAsync(userId, postId);
            return;
        }

        try
        {
            await _redis.SortedSetRemoveAsync(SyncQueue.PostLikersKey(postId), userId);
            await EnqueueAsync(new LikeEvent { UserId = userId, PostId = postId, Action = LikeAction.Unlike });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unlike fast-path failed for user {UserId} post {PostId}; writing straight to SQL", userId, postId);
            await ApplyUnlikeToDbAsync(userId, postId);
        }
    }

    public async Task<(long likesCount, bool isLikedByUser)> GetPostLikesAndIsLikedByUserAsync(long userId, long postId,
        bool useCache = true)
    {
        if (postId <= 0) throw new Exception("Cannot fetch post likes");

        string key = SyncQueue.PostLikersKey(postId);

        if (useCache)
        {
            try
            {
                long cachedCount = await _redis.SortedSetLengthAsync(key);
                if (cachedCount > 0)
                {
                    bool likedInCache = userId > 0 && await _redis.SortedSetScoreAsync(key, userId) is not null;
                    return (cachedCount, likedInCache);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Like read cache failed for post {PostId}; falling back to SQL", postId);
            }
        }

        return await GetPostLikesFromDbAsync(userId, postId, rebuildCache: useCache);
    }

    public async Task<List<long>> GetRecentlyLikedPostIdsByUserAsync(long userId, int page = 1, int pageSize = 10,
        bool useCache = true)
    {
        if (userId <= 0) return [];

        return await dbContext.UserLikes
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => l.PostId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Post>> GetRecentlyLikedPostsByUserAsync(long userId, int page = 1, int limit = 10,
        bool useCache = true)
    {
        var ids = await GetRecentlyLikedPostIdsByUserAsync(userId, page, limit, useCache);
        return await postService.GetPostsByIdListAsync(ids);
    }

    // ── helpers ──────────────────────────────────────────────────────────────────────

    private Task EnqueueAsync(LikeEvent evt) =>
        _redis.SortedSetAddAsync(SyncQueue.LikeEventsKey, SyncQueue.Serialize(evt), SyncQueue.NowScore());

    private async Task<(long likesCount, bool isLikedByUser)> GetPostLikesFromDbAsync(long userId, long postId,
        bool rebuildCache)
    {
        long count = await dbContext.UserLikes.CountAsync(l => l.PostId == postId);
        bool liked = userId > 0 && await dbContext.UserLikes.AnyAsync(l => l.UserId == userId && l.PostId == postId);

        if (rebuildCache && count > 0)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var likers = await dbContext.UserLikes
                        .Where(l => l.PostId == postId)
                        .OrderBy(l => l.CreatedAt)
                        .Select(l => new { l.UserId, l.CreatedAt })
                        .ToListAsync();

                    string key = SyncQueue.PostLikersKey(postId);
                    await _redis.KeyDeleteAsync(key);
                    foreach (var l in likers)
                        await _redis.SortedSetAddAsync(key, l.UserId,
                            (l.CreatedAt.ToUniversalTime() - DateTime.UnixEpoch).TotalMicroseconds);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed rebuilding like cache for post {PostId}", postId);
                }
            });
        }

        return (count, liked);
    }

    private async Task ApplyLikeToDbAsync(long userId, long postId)
    {
        bool exists = await dbContext.UserLikes.AnyAsync(l => l.UserId == userId && l.PostId == postId);
        if (exists) return;
        await dbContext.UserLikes.AddAsync(new UserLike { UserId = userId, PostId = postId });
        await dbContext.SaveChangesAsync();
    }

    private async Task ApplyUnlikeToDbAsync(long userId, long postId)
    {
        var like = await dbContext.UserLikes.FirstOrDefaultAsync(l => l.UserId == userId && l.PostId == postId);
        if (like is null) return;
        dbContext.UserLikes.Remove(like);
        await dbContext.SaveChangesAsync();
    }
}
