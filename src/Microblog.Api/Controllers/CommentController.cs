namespace Microblog.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public class CommentController(ICommentService commentService, IUserService userService) : ControllerBase
{
    [RateLimit(AppConstants.ApiRequestAction.AddComment)]
    [HttpPost]
    public async Task<IActionResult> AddComment([FromBody] CreateUpdateCommentRequestDto createCommentRequest)
    {
        var response = new CommonUtils.ControllerResponseParams();
        long _userId = userService.GetCurrentLoggedInUserId();
        var comment =
            await commentService.AddCommentAsync(_userId, createCommentRequest.PostId, createCommentRequest.Content);
        if (comment == null) throw new Exception("Cannot add comment");
        response.Success = true;
        response.Message = "Comment added successfully";
        response.Data = comment;
        return Ok(response);
    }

    [RateLimit(AppConstants.ApiRequestAction.AddComment)]
    [HttpPut]
    public async Task<IActionResult> UpdateComment([FromBody] CreateUpdateCommentRequestDto createCommentRequest)
    {
        var response = new CommonUtils.ControllerResponseParams();
        long userId = userService.GetCurrentLoggedInUserId();
        var comment =
            await commentService.UpdateCommentAsync(userId, createCommentRequest.CommentId,
                createCommentRequest.Content);
        if (comment == null) throw new Exception("Cannot update comment");
        response.Success = true;
        response.Message = "Comment updated successfully";
        response.Data = comment;
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetCommentsByPostId([FromQuery] long postId)
    {
        var response = new CommonUtils.ControllerResponseParams();
        var comments = await commentService.GetCommentsByPostAsync(postId);
        response.Success = true;
        response.Message = "Comment updated successfully";
        response.Data = comments;
        return Ok(response);
    }
}