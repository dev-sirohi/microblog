using System.Text.Json;

namespace Microblog.Api.Services.BackgroundProcesses;

/// <summary>
/// Central definition of the Redis keys used by the eventual-consistency write path,
/// plus small (de)serialization helpers. Keeping the keys in one place means the write
/// side (services) and the drain side (BackgroundSyncService) can never disagree on them.
/// </summary>
internal static class SyncQueue
{
    // ── Sync queues (Redis sorted sets: member = JSON event, score = enqueue time μs) ──
    public const string LikeEventsKey = "sync:likeEvents";
    public const string FollowEventsKey = "sync:followEvents";

    /// <summary>How many events one drain pass pulls from a queue.</summary>
    public const int BatchSize = 500;

    // ── Read-side caches (serve user-facing reads without hitting SQL) ──
    public static string PostLikersKey(long postId) => $"post:{postId}:likers";        // sorted set of userIds
    public static string UserFollowingKey(long userId) => $"user:{userId}:following";   // set of followingIds
    public static string UserFollowersKey(long userId) => $"user:{userId}:followers";   // set of followerIds

    /// <summary>Monotonic-ish score used both for queue ordering and like recency.</summary>
    public static double NowScore() => (DateTime.UtcNow - DateTime.UnixEpoch).TotalMicroseconds;

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value);
    public static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json)!;
}
