namespace Microblog.Api.Models;

public class UserLike
{
    public long Id { get; set; }
    public long PostId { get; set; }
    public long UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/* Enqueued to Redis and drained to SQL in batches by BackgroundSyncService. */
public class LikeEvent
{
    // Unique id so two otherwise-identical events never collide as sorted-set members.
    public Guid EventId { get; set; } = Guid.NewGuid();
    public long PostId { get; set; }
    public long UserId { get; set; }
    public LikeAction Action { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum LikeAction
{
    Like,
    Unlike
}