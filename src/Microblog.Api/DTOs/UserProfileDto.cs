namespace Microblog.Api.DTOs
{
    public class UserProfileDto
    {
        public long UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public List<Post> UserPosts { get; set; } = new List<Post>();
        public List<Post> RecentlyLikedPosts { get; set; } = new List<Post>();
        public long FollowersCount { get; set; }
        public long FollowingCount { get; set; }
    }
}
