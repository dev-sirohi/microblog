using Microblog.Api.Infrastructure.Observability;

namespace Microblog.Api.Services.BackgroundProcesses;

internal sealed class BackgroundSyncService(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<BackgroundSyncService> logger,
    IServiceProvider serviceProvider) : BackgroundService
{
    private readonly IDatabase _redis = connectionMultiplexer.GetDatabase();
    private readonly TimeSpan _pollDelay = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BackgroundSyncService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await DrainLikesAsync(db);
                await DrainFollowsAsync(db);
                UpdateQueueDepthMetric();
            }
            catch (Exception ex)
            {
                AppMetrics.BackgroundSyncErrors.Inc();
                logger.LogError(ex, "Background sync pass failed");
            }

            await Task.Delay(_pollDelay, stoppingToken);
        }
    }

    private async Task DrainLikesAsync(AppDbContext db)
    {
        var raw = await _redis.SortedSetRangeByRankAsync(SyncQueue.LikeEventsKey, 0, SyncQueue.BatchSize - 1);
        if (raw.Length == 0) return;

        var events = raw.Select(v => SyncQueue.Deserialize<LikeEvent>(v!)).ToList();

        var final = events
            .GroupBy(e => (e.UserId, e.PostId))
            .Select(g => g.OrderBy(e => e.CreatedAt).Last());

        foreach (var e in final)
        {
            bool exists = await db.UserLikes.AnyAsync(l => l.UserId == e.UserId && l.PostId == e.PostId);
            if (e.Action == LikeAction.Like && !exists)
                await db.UserLikes.AddAsync(new UserLike { UserId = e.UserId, PostId = e.PostId, CreatedAt = e.CreatedAt });
            else if (e.Action == LikeAction.Unlike && exists)
                db.UserLikes.RemoveRange(db.UserLikes.Where(l => l.UserId == e.UserId && l.PostId == e.PostId));
        }

        await db.SaveChangesAsync();
        await _redis.SortedSetRemoveRangeByRankAsync(SyncQueue.LikeEventsKey, 0, raw.Length - 1);
        AppMetrics.BackgroundSyncProcessed.Inc(raw.Length);
    }

    private async Task DrainFollowsAsync(AppDbContext db)
    {
        var raw = await _redis.SortedSetRangeByRankAsync(SyncQueue.FollowEventsKey, 0, SyncQueue.BatchSize - 1);
        if (raw.Length == 0) return;

        var events = raw.Select(v => SyncQueue.Deserialize<FollowEvent>(v!)).ToList();

        var final = events
            .GroupBy(e => (e.FollowerId, e.FollowingId))
            .Select(g => g.OrderBy(e => e.CreatedAt).Last());

        foreach (var e in final)
        {
            bool exists = await db.UserFollows.AnyAsync(f => f.FollowerId == e.FollowerId && f.FollowingId == e.FollowingId);
            if (e.Action == FollowAction.Follow && !exists)
                await db.UserFollows.AddAsync(new UserFollow { FollowerId = e.FollowerId, FollowingId = e.FollowingId, CreatedAt = e.CreatedAt });
            else if (e.Action == FollowAction.Unfollow && exists)
                db.UserFollows.RemoveRange(db.UserFollows.Where(f => f.FollowerId == e.FollowerId && f.FollowingId == e.FollowingId));
        }

        await db.SaveChangesAsync();
        await _redis.SortedSetRemoveRangeByRankAsync(SyncQueue.FollowEventsKey, 0, raw.Length - 1);
        AppMetrics.BackgroundSyncProcessed.Inc(raw.Length);
    }

    private void UpdateQueueDepthMetric()
    {
        try
        {
            long depth = _redis.SortedSetLength(SyncQueue.LikeEventsKey) + _redis.SortedSetLength(SyncQueue.FollowEventsKey);
            AppMetrics.BackgroundSyncQueueDepth.Set(depth);
        }
        catch {  }
    }
}
