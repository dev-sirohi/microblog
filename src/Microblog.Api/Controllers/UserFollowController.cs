using Microblog.Api.Infrastructure.Messaging;

namespace Microblog.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UserFollowController(
    UserFollowService userFollowService,
    UserService userService,
    IServiceProvider serviceProvider)
    : ControllerBase
{
    [RateLimit(AppConstants.ApiRequestAction.Follow)]
    [HttpPost("follow/{userId:long}")]
    public async Task<IActionResult> FollowUser(long userId)
    {
        var response = new CommonUtils.ControllerResponseParams();

        long loggedInUserId = userService.GetCurrentLoggedInUserId();
        await userFollowService.FollowUserAsync(loggedInUserId, userId);

        _ = Task.Run(async () =>
        {
            try
            {
                var publisher = serviceProvider.GetService<IMessagePublisher>();
                if (publisher is not null)
                    await publisher.PublishAsync("user.followed",
                        new UserFollowedEvent(loggedInUserId, userId, true, DateTime.UtcNow));
            }
            catch { }
        });

        response.Success = true;
        response.Message = "Followed user";

        return Ok(response);
    }

    [RateLimit(AppConstants.ApiRequestAction.Unfollow)]
    [HttpPost("unfollow/{userId:long}")]
    public async Task<IActionResult> UnfollowUser(long userId)
    {
        var response = new CommonUtils.ControllerResponseParams();

        long loggedInUserId = userService.GetCurrentLoggedInUserId();
        await userFollowService.UnfollowUserAsync(loggedInUserId, userId);

        response.Success = true;
        response.Message = "Unfollowed user";

        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetFollowers()
    {
        var response = new CommonUtils.ControllerResponseParams();

        long loggedInUserId = userService.GetCurrentLoggedInUserId();
        var followerIdList = await userFollowService.GetFollowerIdListAsync(loggedInUserId);
        var followerList = await userService.GetUserListByIdListAsync(followerIdList);

        response.Success = true;
        response.Data = followerList.Select(u => new { u.Id, u.Username });

        return Ok(response);
    }

    [HttpGet("following")]
    public async Task<IActionResult> GetFollowing()
    {
        var response = new CommonUtils.ControllerResponseParams();

        long loggedInUserId = userService.GetCurrentLoggedInUserId();
        var followingIdList = await userFollowService.GetFollowingIdListAsync(loggedInUserId);
        var followingList = await userService.GetUserListByIdListAsync(followingIdList);

        response.Success = true;
        response.Data = followingList.Select(u => new { u.Id, u.Username });

        return Ok(response);
    }
}
