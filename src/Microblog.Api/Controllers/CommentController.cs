namespace Microblog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly IUserService _userService;
        private readonly IRateLimiter _rateLimiter;

        public CommentController(ICommentService commentService, IUserService userService, IRateLimiter rateLimiter)
        {
            _commentService = commentService;
            _userService = userService;
            _rateLimiter = rateLimiter;
        }

        [HttpPost("addcomment")]
        public async Task<IActionResult> AddComment([FromBody] CreateUpdateCommentRequestDto createCommentRequest)
        {
            CommonUtils.ControllerResponseParams response = new CommonUtils.ControllerResponseParams();
            if (await _rateLimiter.IsRequestAllowedAsync(AppConstants.ApiRequestAction.AddComment))
            {
                long userId = _userService.GetCurrentLoggedInUserId();
                Comment? comment = await _commentService.AddCommentAsync(userId, createCommentRequest.PostId, createCommentRequest.Content);
                if (comment == null)
                {
                    throw new Exception("Cannot add comment");
                }
                response.Success = true;
                response.Message = "Comment added succesfully";
                response.Data = comment;
            }
            return Ok(response);
        }

        [HttpPost("updatecomment")]
        public async Task<IActionResult> UpdateComment([FromBody] CreateUpdateCommentRequestDto createCommentRequest)
        {
            CommonUtils.ControllerResponseParams response = new CommonUtils.ControllerResponseParams();
            if (await _rateLimiter.IsRequestAllowedAsync(AppConstants.ApiRequestAction.AddComment))
            {
                long userId = _userService.GetCurrentLoggedInUserId();
                Comment? comment = await _commentService.UpdateCommentAsync(userId, createCommentRequest.CommentId, createCommentRequest.Content);
                if (comment == null)
                {
                    throw new Exception("Cannot update comment");
                }
                response.Success = true;
                response.Message = "Comment updated succesfully";
                response.Data = comment;
            }
            return Ok(response);
        }

        [HttpPost("getcommentsbypostid")]
        public async Task<IActionResult> GetCommentsByPostId([FromQuery] long postId)
        {
            CommonUtils.ControllerResponseParams response = new CommonUtils.ControllerResponseParams();
            List<Comment> comments = await _commentService.GetCommentsByPostAsync(postId);
            response.Success = true;
            response.Message = "Comment updated succesfully";
            response.Data = comments;
            return Ok(response);
        }
    }
}
