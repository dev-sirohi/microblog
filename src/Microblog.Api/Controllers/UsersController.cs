using Microblog.Api.Infrastructure.Storage;

namespace Microblog.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController(
    UserService userService,
    PostService postService,
    UserFollowService userFollowService,
    IStorageService storageService) : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var response = new CommonUtils.ControllerResponseParams();
        var user = await userService.GetCurrentLoggedInUserAsync();
        response.Success = true;
        response.Data = new
        {
            user.Id,
            user.Username,
            user.Bio,
            AvatarUrl = string.IsNullOrEmpty(user.ProfilePictureUrl)
                ? string.Empty
                : storageService.GetUrl(user.ProfilePictureUrl)
        };
        return Ok(response);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetProfile(long id)
    {
        var response = new CommonUtils.ControllerResponseParams();
        var user = await userService.GetUserByIdAsync(id);
        var posts = await postService.GetUserPostsAsync(id);
        var followers = await userFollowService.GetFollowerIdListAsync(id);
        var following = await userFollowService.GetFollowingIdListAsync(id);

        bool isFollowing = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            long me = userService.GetCurrentLoggedInUserId();
            isFollowing = followers.Contains(me);
        }

        response.Success = true;
        response.Data = new
        {
            user.Id,
            user.Username,
            user.Bio,
            AvatarUrl = string.IsNullOrEmpty(user.ProfilePictureUrl)
                ? string.Empty
                : storageService.GetUrl(user.ProfilePictureUrl),
            FollowersCount = followers.Count,
            FollowingCount = following.Count,
            IsFollowing = isFollowing,
        };
        return Ok(response);
    }

    [Authorize]
    [HttpPost("me/avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var response = new CommonUtils.ControllerResponseParams();
        if (file is null || file.Length == 0) throw new AppException("No file uploaded");

        long userId = userService.GetCurrentLoggedInUserId();
        string blobName = $"avatars/{userId}{Path.GetExtension(file.FileName)}";
        string storedPath = await storageService.SaveFileAsync(file, blobName);
        await userService.SetProfilePictureUrlAsync(userId, storedPath);

        response.Success = true;
        response.Message = "Avatar uploaded successfully";
        response.Data = new { AvatarUrl = storageService.GetUrl(storedPath) };
        return Ok(response);
    }
}
