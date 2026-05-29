namespace Microblog.Api.Utils;

public class AppConstants
{
    public enum ApiRequestAction
    {
        Login,
        Register,
        Logout,
        CreateUser,
        UpdateUser,
        DeleteUser,
        CreatePost,
        UpdatePost,
        DeletePost,
        AddComment,
        UpdateComment,
        DeleteComment,
        Follow,
        Unfollow,
        LikePost,
        UnlikePost
    }

    /* Any additions to this must also be added to InMemoryUtils.cs */
    public enum InMemoryOperationType
    {
        USER_FOLLOWING,
        USER_FOLLOWED_BY,
        USER_FOLLOWING_COUNT,
        USER_FOLLOWER_COUNT,
        POST_LIKES_INCREASED_BY_USER_ID,
        UNLIKE_POST,
        LIKE_EVENT_FOR_BACKGROUND_SYNC_QUEUE_ADD,
        LIKE_EVENT_QUEUE_REMOVE,
        ADD_MEDIA_FILE_PATH,
        USER_RECENTLY_LIKED_POST
    }

    public enum MediaEntityType
    {
        User = 1,
        Post = 2
    }

    public const string BASE_URL = "https://microblog.com";

    public static Dictionary<InMemoryOperationType, CacheConfig> CacheConfigDict = new()
    {
        {
            InMemoryOperationType.POST_LIKES_INCREASED_BY_USER_ID,
            new CacheConfig
            {
                CacheTTLSeconds = TimeSpan.FromHours(12),
                CacheMemoryLimit = 100 * 100 * 10,
                CacheTrimBatch = 1000
            }
        },
        {
            InMemoryOperationType.LIKE_EVENT_FOR_BACKGROUND_SYNC_QUEUE_ADD,
            new CacheConfig
            {
                CacheMemoryLimit = 100 * 100 * 10,
                CacheTrimBatch = 1000
            }
        },
        {
            InMemoryOperationType.USER_RECENTLY_LIKED_POST,
            new CacheConfig
            {
                CacheMemoryLimit = 100 * 10,
                CacheTrimBatch = 100
            }
        }
    };

    public class InMemoryPubSubChannels
    {
        public static string FlushAndClearQueueOverflow = "FlushAndClearQueueOverflow";
    }

    public class CacheConfig
    {
        public TimeSpan CacheTTLSeconds { get; set; } = TimeSpan.FromSeconds(5);
        public int CacheMemoryLimit { get; set; } = 1000;
        public long CacheTrimBatch { get; set; } = 100;
    }
}