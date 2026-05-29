namespace Microblog.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserLikeController(
    IUserService userService,
    IUserLikeService userLikeService,
    IRateLimiter rateLimiter)
    : ControllerBase
{
    [RateLimit(AppConstants.ApiRequestAction.LikePost)]
    [HttpPost("like/{id:long}")]
    public async Task<IActionResult> LikePost(long id)
    {
        var response = new CommonUtils.ControllerResponseParams();
        if (await rateLimiter.IsRequestAllowedAsync(AppConstants.ApiRequestAction.LikePost))
        {
            long userId = userService.GetCurrentLoggedInUserId();
            await userLikeService.LikePostAsync(userId, id);
        }

        response.Success = true;
        return Ok(response);
    }

    [RateLimit(AppConstants.ApiRequestAction.LikePost)]
    [HttpPost("unlike/{id:long}")]
    public async Task<IActionResult> UnlikePost(long id)
    {
        var response = new CommonUtils.ControllerResponseParams();

        long userId = userService.GetCurrentLoggedInUserId();
        await userLikeService.UnlikePostAsync(userId, id);

        response.Success = true;

        return Ok(response);
    }
}