using System.Text.Json;

namespace Microblog.Api.Services.BackgroundProcesses;

internal static class SyncQueue
{
    public const string LikeEventsKey = "sync:likeEvents";
    public const string FollowEventsKey = "sync:followEvents";

    public const int BatchSize = 500;

    public static string PostLikersKey(long postId) => $"post:{postId}:likers";
    public static string UserFollowingKey(long userId) => $"user:{userId}:following";
    public static string UserFollowersKey(long userId) => $"user:{userId}:followers";

    public static double NowScore() => (DateTime.UtcNow - DateTime.UnixEpoch).TotalMicroseconds;

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value);
    public static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json)!;
}
