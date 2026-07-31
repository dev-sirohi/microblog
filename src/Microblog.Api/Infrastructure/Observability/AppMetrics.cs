using Prometheus;

namespace Microblog.Api.Infrastructure.Observability;

public sealed class AppMetrics
{
    public static readonly Counter CacheHits =
        Metrics.CreateCounter("microblog_cache_hits_total", "Total cache hits", labelNames: ["operation"]);

    public static readonly Counter CacheMisses =
        Metrics.CreateCounter("microblog_cache_misses_total", "Total cache misses", labelNames: ["operation"]);

    public static readonly Gauge BackgroundSyncQueueDepth =
        Metrics.CreateGauge("microblog_background_sync_queue_depth", "Current depth of the background sync event queue");

    public static readonly Counter BackgroundSyncProcessed =
        Metrics.CreateCounter("microblog_background_sync_processed_total", "Events processed by the background sync service");

    public static readonly Counter BackgroundSyncErrors =
        Metrics.CreateCounter("microblog_background_sync_errors_total", "Errors during background sync processing");

    public static readonly Counter PostsCreated =
        Metrics.CreateCounter("microblog_posts_created_total", "Total posts created");

    public static readonly Counter PostLikes =
        Metrics.CreateCounter("microblog_post_likes_total", "Total post like events");

    public static readonly Counter MessagesPublished =
        Metrics.CreateCounter("microblog_messages_published_total", "Messages published to the event bus", labelNames: ["topic"]);

    public static readonly Counter MessageErrors =
        Metrics.CreateCounter("microblog_message_errors_total", "Message publish/consume errors", labelNames: ["topic"]);

    public static readonly Gauge ActiveSessions =
        Metrics.CreateGauge("microblog_active_sessions", "Approximate active authenticated sessions");
}
