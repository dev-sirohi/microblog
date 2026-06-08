using Microsoft.AspNetCore.RateLimiting;
using Microblog.Api.Infrastructure.Messaging;

namespace Microblog.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserLikeController(
    IUserService userService,
    IUserLikeService userLikeService,
    IServiceProvider serviceProvider)
    : ControllerBase
{
    [EnableRateLimiting("create-post")]
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

    [EnableRateLimiting("create-post")]
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
