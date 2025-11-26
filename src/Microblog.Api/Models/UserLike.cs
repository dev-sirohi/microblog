namespace Microblog.Api.Models
{
    public class UserLike
    {
        public long Id { get; set; }
        public long PostId { get; set; }
        public long UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public Post? Post { get; set; }
    }

    /* For caching/DB sync */
    public class LikeEvent
    {
        public long PostId { get; set; }
        public long UserId { get; set; }
        public LikeAction Action { get; set; }
        public DateTime CreatedAt { get; set;} = DateTime.UtcNow;
    }

    public enum LikeAction
    {
        Like,
        Unlike,
    }
}
