namespace Microblog.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController(
    IUserService userService,
    IPostService postService,
    IUserFollowService userFollowService) : ControllerBase
{
    /// <summary>The currently authenticated user (used by the client to know who is logged in).</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var response = new CommonUtils.ControllerResponseParams();
        var user = await userService.GetCurrentLoggedInUserAsync();
        response.Success = true;
        response.Data = new { user.Id, user.Username, user.Bio };
        return Ok(response);
    }

    /// <summary>A user's public profile: their posts plus follower/following counts.</summary>
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
            FollowersCount = followers.Count,
            FollowingCount = following.Count,
            IsFollowing = isFollowing,
            Posts = posts
        };
        return Ok(response);
    }
}
