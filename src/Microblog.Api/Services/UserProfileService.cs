using Microblog.Api.Interfaces.ServiceInterfaces;

namespace Microblog.Api.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly AppDbContext _dbContext;
        private readonly IDatabase _inMemoryDb;
        private readonly IUserService _userService;
        private readonly IMediaService _mediaService;
        private readonly IPostService _postService;
        private readonly IUserLikeService _userLikeService;

        public UserProfileService(AppDbContext dbContext, IConnectionMultiplexer connectionMultiplexer, IUserService userService, IMediaService mediaService, IUserLikeService userLikeService)
        {
            _dbContext = dbContext;
            _inMemoryDb = connectionMultiplexer.GetDatabase();
            _userService = userService;
            _mediaService = mediaService;
            _userLikeService = userLikeService;
        }

        public async Task<UserProfileDto> GetUserProfileByUserIdAsync(long userId)
        {
            long followersCount = await _userService.GetUserFollowerCountAsync(userId);
            long followingCount = await _userService.GetUserFollowingCountAsync(userId);
            string profilePictureUrl = CommonUtils.BuildMediaUrl(await _mediaService.GetMediaFilePathAsync(userId, AppConstants.MediaEntityType.User));
            List<Post> recentlyLikedPosts = await _userLikeService.GetRecentlyLikedPostsByUserAsync(userId);
            List<Post> userPosts = await _postService.GetUserPostsAsync(userId);

            User userObj = await _userService.GetUserByIdAsync(userId);

            UserProfileDto userProfile = new UserProfileDto
            {
                UserId = userId,
                Username = userObj.Username,
                Bio = userObj.Bio,
                AvatarUrl = profilePictureUrl,
                UserPosts = new List<Post>(),
                RecentlyLikedPosts = new List<Post>(),
                FollowersCount = followersCount,
                FollowingCount = followingCount,
            };

            return userProfile;
        }
    }
}
