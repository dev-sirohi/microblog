using Microsoft.AspNetCore.RateLimiting;
using Microblog.Api.Infrastructure.Messaging;

namespace Microblog.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UserFollowController(
    IUserFollowService userFollowService,
    IUserService userService,
    IServiceProvider serviceProvider)
    : ControllerBase
{
    [EnableRateLimiting("create-post")]
    [HttpPost("follow/{userId:long}")]
    public async Task<IActionResult> FollowUser(long userId)
    {
        var response = new CommonUtils.ControllerResponseParams();

        long loggedInUserId = userService.GetCurrentLoggedInUserId();
        await userFollowService.FollowUserAsync(loggedInUserId, userId);
        var user = await userService.GetUserByIdAsync(userId);

        _ = Task.Run(async () =>
        {
            try
            {
                var publisher = serviceProvider.GetService<IMessagePublisher>();
                if (publisher is not null)
                    await publisher.PublishAsync("user.followed", new UserFollowedEvent(loggedInUserId, userId, true, DateTime.UtcNow));
            }
            catch { }
        });

        response.Success = true;
        response.Message = "Following user " + user?.Username;

        return Ok(response);
    }

    [EnableRateLimiting("create-post")]
    [HttpPost("unfollow/{userId:long}")]
    public async Task<IActionResult> UnfollowUser(long userId)
    {
        var response = new CommonUtils.ControllerResponseParams();

        long loggedInUserId = userService.GetCurrentLoggedInUserId();
        await userFollowService.UnfollowUserAsync(loggedInUserId, userId);
        var user = await userService.GetUserByIdAsync(userId);

        response.Success = true;
        response.Message = "Unfollowed user " + user?.Username;

        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetFollowers()
    {
        var response = new CommonUtils.ControllerResponseParams();

        long loggedInUserId = userService.GetCurrentLoggedInUserId();
        var followerIdList = await userFollowService.GetFollowerIdListAsync(loggedInUserId);
        var followerList = await userService.GetUserListByIdListReadOnlyAsync(followerIdList);
        var followerResponseList = CommonUtils.TransformTo<IReadOnlyCollection<UserResponseDto>>(followerList);

        response.Success = true;
        response.Data = followerResponseList;

        return Ok(response);
    }

    [HttpGet("{userId:long}")]
    public async Task<IActionResult> GetFollowers(long userId)
    {
        var response = new CommonUtils.ControllerResponseParams();

        var followerIdList = await userFollowService.GetFollowerIdListAsync(userId);
        var followerList = await userService.GetUserListByIdListReadOnlyAsync(followerIdList);
        var followerResponseList = CommonUtils.TransformTo<IReadOnlyCollection<UserResponseDto>>(followerList);

        response.Success = true;
        response.Data = followerResponseList;

        return Ok(response);
    }

    [HttpGet("following")]
    public async Task<IActionResult> GetFollowing()
    {
        var response = new CommonUtils.ControllerResponseParams();

        long loggedInUserId = userService.GetCurrentLoggedInUserId();
        var followingIdList = await userFollowService.GetFollowingIdListAsync(loggedInUserId);
        var followingUsersList = await userService.GetUserListByIdListReadOnlyAsync(followingIdList);
        var followingUsersResponseList =
            CommonUtils.TransformTo<IReadOnlyCollection<UserResponseDto>>(followingUsersList);

        response.Success = true;
        response.Data = followingUsersResponseList;

        return Ok(response);
    }

    [HttpGet("following/{userId:long}")]
    public async Task<IActionResult> GetFollowing(long userId)
    {
        var response = new CommonUtils.ControllerResponseParams();

        var followingIdList = await userFollowService.GetFollowingIdListAsync(userId);
        var followingUsersList = await userService.GetUserListByIdListReadOnlyAsync(followingIdList);
        var followingUsersResponseList =
            CommonUtils.TransformTo<IReadOnlyCollection<UserResponseDto>>(followingUsersList);

        response.Success = true;
        response.Data = followingUsersResponseList;

        return Ok(response);
    }
}
