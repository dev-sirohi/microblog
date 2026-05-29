namespace Microblog.Api.Services;

public class UserProfileService : IUserProfileService
{
    private readonly AppDbContext _dbContext;
    private readonly IDatabase _inMemoryDb;
    private readonly IMediaService _mediaService;
    private readonly IPostService _postService;
    private readonly IUserLikeService _userLikeService;
    private readonly IUserService _userService;

    public UserProfileService(AppDbContext dbContext, IConnectionMultiplexer connectionMultiplexer,
        IUserService userService, IMediaService mediaService, IUserLikeService userLikeService)
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
        string profilePictureUrl =
            CommonUtils.BuildMediaUrl(
                await _mediaService.GetMediaFilePathAsync(userId, AppConstants.MediaEntityType.User));
        var recentlyLikedPosts = await _userLikeService.GetRecentlyLikedPostsByUserAsync(userId);
        var userPosts = await _postService.GetUserPostsAsync(userId);

        var userObj = await _userService.GetUserByIdAsync(userId);

        var userProfile = new UserProfileDto
        {
            UserId = userId,
            Username = userObj.Username,
            Bio = userObj.Bio,
            AvatarUrl = profilePictureUrl,
            UserPosts = new List<Post>(),
            RecentlyLikedPosts = new List<Post>(),
            FollowersCount = followersCount,
            FollowingCount = followingCount
        };

        return userProfile;
    }
}