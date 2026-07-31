namespace Microblog.Api.Models;

public class UserFollow
{
    public long Id { get; set; }
    public long FollowerId { get; set; }
    public long FollowingId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/* Enqueued to Redis and drained to SQL in batches by BackgroundSyncService. */
public class FollowEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public long FollowerId { get; set; }
    public long FollowingId { get; set; }
    public FollowAction Action { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum FollowAction
{
    Follow,
    Unfollow
}