namespace Microblog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserLikeController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserLikeService _userLikeService;
        private IRateLimiter _rateLimiter;
        public UserLikeController(IUserService userService, IUserLikeService userLikeService, IRateLimiter rateLimiter)
        {
            _userService = userService;
            _userLikeService = userLikeService;
            _rateLimiter = rateLimiter;
        }

        [HttpPost("likepost")]
        public async Task<IActionResult> LikePost([FromQuery] long postId)
        {
            CommonUtils.ControllerResponseParams response = new CommonUtils.ControllerResponseParams();
            if (await _rateLimiter.IsRequestAllowedAsync(AppConstants.ApiRequestAction.LikePost))
            {
                long userId = _userService.GetCurrentLoggedInUserId();
                await _userLikeService.LikePostAsync(userId, postId);
            }
            response.Success = true;
            return Ok(response);
        }

        [HttpPost("unlikepost")]
        public async Task<IActionResult> UnlikePost([FromQuery] long postId)
        {
            CommonUtils.ControllerResponseParams response = new CommonUtils.ControllerResponseParams();
            if (await _rateLimiter.IsRequestAllowedAsync(AppConstants.ApiRequestAction.LikePost))
            {
                long userId = _userService.GetCurrentLoggedInUserId();
                await _userLikeService.UnlikePostAsync(userId, postId);
            }
            response.Success = true;
            return Ok(response);
        }
    }
}
