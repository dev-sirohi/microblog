namespace Microblog.Api.Models
{
    public class UserFollow
    {
        public long Id { get; set; }
        public long FollowerId { get; set; }
        public long FollowingId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? Follower { get; set; }
        public User? Following { get; set; }
    }
}
