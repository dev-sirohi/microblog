using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Microblog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserFollowController : ControllerBase
    {
        private readonly IUserFollowService _userFollowService;
        private readonly IUserService _userService;
        private readonly IRateLimiter _rateLimiter;

        public UserFollowController(IUserFollowService userFollowService, IUserService userService, IRateLimiter rateLimiter)
        {
            _userFollowService = userFollowService;
            _userService = userService;
            _rateLimiter = rateLimiter;
        }

        [HttpPost("follow")]
        public async Task<IActionResult> FollowUser([FromQuery] long userId)
        {
            CommonUtils.ControllerResponseParams response = new CommonUtils.ControllerResponseParams();
            long loggedInUserId = _userService.GetCurrentLoggedInUserId();
            if (await _rateLimiter.IsRequestAllowedAsync(AppConstants.ApiRequestAction.Follow))
            {
                await _userFollowService.FollowUserAsync(loggedInUserId, userId);
            }
            User? user = await _userService.GetUserByIdAsync(userId);
            response.Success = true;
            response.Message = "Following user " + user?.Username;
            return Ok(response);
        }

        [HttpPost("unfollow")]
        public async Task<IActionResult> UnfollowUser([FromQuery] long followingUserId)
        {
            CommonUtils.ControllerResponseParams response = new CommonUtils.ControllerResponseParams();
            long loggedInUserId = _userService.GetCurrentLoggedInUserId();
            if (await _rateLimiter.IsRequestAllowedAsync(AppConstants.ApiRequestAction.Unfollow))
            {
                await _userFollowService.UnfollowUserAsync(loggedInUserId, followingUserId);
            }
            User? user = await _userService.GetUserByIdAsync(followingUserId);
            response.Success = true;
            response.Message = "Unfollowed user " + user?.Username;
            return Ok(response);
        }

        [HttpGet("getfollowers")]
        public async Task<IActionResult> GetFollowers()
        {
            CommonUtils.ControllerResponseParams response = new CommonUtils.ControllerResponseParams();
            long loggedInUserId = _userService.GetCurrentLoggedInUserId();
            IReadOnlyCollection<long> followerIdList = await _userFollowService.GetFollowerIdListAsync(loggedInUserId);
            IReadOnlyCollection<User> followerList = await _userService.GetUserListByIdListReadOnlyAsync(followerIdList);
            IReadOnlyCollection<UserResponseDto>? followerResponseList = CommonUtils.TransformTo<IReadOnlyCollection<UserResponseDto>>(followerList);
            response.Success = true;
            response.Data = followerResponseList;
            return Ok(response);
        }

        [HttpGet("getfollowing")]
        public async Task<IActionResult> GetFollowing()
        {
            CommonUtils.ControllerResponseParams response = new CommonUtils.ControllerResponseParams();
            long loggedInUserId = _userService.GetCurrentLoggedInUserId();
            IReadOnlyCollection<long> followingIdList = await _userFollowService.GetFollowingIdListAsync(loggedInUserId);
            IReadOnlyCollection<User> followingUsersList = await _userService.GetUserListByIdListReadOnlyAsync(followingIdList);
            IReadOnlyCollection<UserResponseDto>? followingUsersResponseList = CommonUtils.TransformTo<IReadOnlyCollection<UserResponseDto>>(followingUsersList);
            response.Success = true;
            response.Data = followingUsersResponseList;
            return Ok(response);
        }
    }
}
