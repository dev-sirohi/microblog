using Microblog.Api.Infrastructure.Messaging;

namespace Microblog.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UserLikeController(
    UserService userService,
    UserLikeService userLikeService,
    IServiceProvider serviceProvider)
    : ControllerBase
{
    [RateLimit(AppConstants.ApiRequestAction.LikePost)]
    [HttpPost("like/{id:long}")]
    public async Task<IActionResult> LikePost(long id)
    {
        var response = new CommonUtils.ControllerResponseParams();
        long userId = userService.GetCurrentLoggedInUserId();
        await userLikeService.LikePostAsync(userId, id);

        _ = Task.Run(async () =>
        {
            try
            {
                var publisher = serviceProvider.GetService<IMessagePublisher>();
                if (publisher is not null)
                    await publisher.PublishAsync("post.liked", new PostLikedEvent(id, userId, true, DateTime.UtcNow));
            }
            catch { }
        });

        response.Success = true;
        return Ok(response);
    }

    [RateLimit(AppConstants.ApiRequestAction.UnlikePost)]
    [HttpPost("unlike/{id:long}")]
    public async Task<IActionResult> UnlikePost(long id)
    {
        var response = new CommonUtils.ControllerResponseParams();
        long userId = userService.GetCurrentLoggedInUserId();
        await userLikeService.UnlikePostAsync(userId, id);

        response.Success = true;
        return Ok(response);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetPostLikes(long id)
    {
        var response = new CommonUtils.ControllerResponseParams();
        long userId = userService.GetCurrentLoggedInUserId();
        var (likesCount, isLikedByUser) = await userLikeService.GetPostLikesAndIsLikedByUserAsync(userId, id);

        response.Success = true;
        response.Message = "Post likes fetched successfully";
        response.Data = new { LikesCount = likesCount, IsLikedByUser = isLikedByUser };
        return Ok(response);
    }
}
