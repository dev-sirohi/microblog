namespace Microblog.Api.Services.BackgroundProcesses;

internal sealed class SyncWorkerService
{
    private readonly IAuthService? _authService;
    private readonly long _batchSize = 1000;
    private readonly ICommentService? _commentService;
    private readonly AppDbContext _dbContext;
    private readonly IDatabase _inMemoryDb;
    private readonly IPostService? _postService;
    private readonly IUserFollowService? _userFollowService;
    private readonly IUserLikeService? _userLikeService;
    private readonly IUserService? _userService;

    public SyncWorkerService(
        BackgroundSyncService.BackgroundSyncToken token,
        Guid expectedSecret,
        IDatabase inMemoryDb,
        AppDbContext dbContext,
        long batchSize = 1000,
        IUserService? userService = null,
        IUserLikeService? userLikeService = null
    )
    {
        if (token.Secret != expectedSecret) throw new Exception("Invalid caller for SyncWorkerService");
        _inMemoryDb = inMemoryDb;
        _dbContext = dbContext;
        _batchSize = batchSize;
    }

    internal async Task SyncPostLikes()
    {
        string key = InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.LIKE_EVENT_FOR_BACKGROUND_SYNC_QUEUE_ADD);
        long count = await _inMemoryDb.SortedSetLengthAsync(key);

        if (count == 0) return;

        var likeEvents =
            InMemoryUtils.GetMembersFromValueListAs<LikeEvent>(
                await _inMemoryDb.SortedSetRangeByRankAsync(key, 0, _batchSize - 1));
        var finalLikeEvents = likeEvents
            .OrderBy(e => e.CreatedAt)
            .GroupBy(e => new { e.UserId, e.PostId }).Select(g => g.Last())
            .ToList();
        var userLikesToInsert = finalLikeEvents.Select(e => new UserLike
        {
            UserId = e.UserId,
            PostId = e.PostId,
            CreatedAt = e.CreatedAt
        }).ToList();

        await _dbContext.UserLikes.AddRangeAsync(userLikesToInsert);
        await _dbContext.SaveChangesAsync();
    }
}