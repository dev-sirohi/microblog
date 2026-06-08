using Microsoft.AspNetCore.RateLimiting;
using Microblog.Api.Features.Recommendations;

namespace Microblog.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PostController(
    IPostService postService,
    IUserService userService,
    IServiceProvider serviceProvider) : ControllerBase
{
    [EnableRateLimiting("create-post")]
    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreateUpdatePostRequestDto createPostRequest)
    {
        var response = new CommonUtils.ControllerResponseParams();
        long userId = userService.GetCurrentLoggedInUserId();
        var post = await postService.CreatePostAsync(userId, createPostRequest.Content);
        if (post == null) throw new Exception("Cannot create post");
        response.Success = true;
        response.Message = "Post created successfully";
        response.Data = post;
        return Ok(response);
    }

    [EnableRateLimiting("feed")]
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetPostById(long id)
    {
        var response = new CommonUtils.ControllerResponseParams();
        var post = await postService.GetPostByIdAsync(id);
        if (post == null) throw new Exception("Cannot fetch post");
        response.Success = true;
        response.Message = "Post fetched successfully";
        response.Data = post;
        return Ok(response);
    }

    [EnableRateLimiting("feed")]
    [HttpGet("homefeed")]
    public Task<IActionResult> GetHomeFeed()
    {
        try
        {
            var response = new CommonUtils.ControllerResponseParams();
            var posts = new List<Post>();
            response.Success = true;
            response.Message = "Post fetched successfully";
            response.Data = posts;
            return Task.FromResult<IActionResult>(Ok(response));
        }
        catch (Exception exception)
        {
            return Task.FromException<IActionResult>(exception);
        }
    }

    [EnableRateLimiting("create-post")]
    [HttpPatch("{id:long}")]
    public async Task<IActionResult> UpdatePost(long id, [FromBody] CreateUpdatePostRequestDto updatePostRequest)
    {
        var response = new CommonUtils.ControllerResponseParams();
        long userId = userService.GetCurrentLoggedInUserId();
        var post = await postService.UpdatePostAsync(id, userId, updatePostRequest.Content);
        if (post == null) throw new Exception("Cannot update post");
        response.Success = true;
        response.Message = "Post updated successfully";
        response.Data = post;
        return Ok(response);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeletePostById(long id)
    {
        var response = new CommonUtils.ControllerResponseParams();
        long userId = userService.GetCurrentLoggedInUserId();
        await postService.DeletePostAsync(id, userId);
        response.Success = true;
        response.Message = "Post deleted successfully";
        return Ok(response);
    }

    [EnableRateLimiting("feed")]
    [HttpGet("{id:long}/recommendations")]
    public async Task<IActionResult> GetRecommendations(long id, [FromQuery] int limit = 5)
    {
        var response = new CommonUtils.ControllerResponseParams();
        var recommendations = serviceProvider.GetService<IRecommendationService>();
        if (recommendations is null)
        {
            response.Success = false;
            response.Message = "Recommendations feature is disabled. Enable Features:EnableEmbeddings in configuration.";
            return Ok(response);
        }

        var posts = await recommendations.GetRecommendationsAsync(id, Math.Clamp(limit, 1, 10));
        response.Success = true;
        response.Message = "Recommendations fetched successfully";
        response.Data = posts;
        return Ok(response);
    }
}
