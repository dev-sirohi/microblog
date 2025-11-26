namespace Microblog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly IRateLimiter _rateLimiter;
        private readonly IUserService _userService;

        public PostController(IPostService postService, IRateLimiter rateLimiter, IUserService userService)
        {
            _postService = postService;
            _rateLimiter = rateLimiter;
            _userService = userService;
        }

        [HttpPost("createpost")]
        public async Task<IActionResult> CreatePost([FromBody] CreateUpdatePostRequestDto createPostRequest)
        {
            CommonUtils.ControllerResponseParams response = new CommonUtils.ControllerResponseParams();
            if (await _rateLimiter.IsRequestAllowedAsync(AppConstants.ApiRequestAction.CreatePost))
            {
                long userId = _userService.GetCurrentLoggedInUserId();
                Post? post = await _postService.CreatePostAsync(userId, createPostRequest.Content);
                if (post == null)
                {
                    throw new Exception("Cannot create post");
                }
                response.Success = true;
                response.Message = "Post created successfully";
                response.Data = post;
            }
            return Ok(response);
        }

        [HttpGet("getpostbyid")]
        public async Task<IActionResult> GetPostById([FromBody] long postId)
        {
            CommonUtils.ControllerResponseParams response = new CommonUtils.ControllerResponseParams();
            Post? post = await _postService.GetPostByIdAsync(postId);
            if (post == null)
            {
                throw new Exception("Cannot fetch post");
            }
            response.Success = true;
            response.Message = "Post fetched successfully";
            response.Data = post;
            return Ok(response);
        }

        [HttpGet("gethomefeed")]
        public async Task<IActionResult> GetHomeFeed()
        {
            CommonUtils.ControllerResponseParams response = new CommonUtils.ControllerResponseParams();
            List<Post>? posts = new List<Post>();
            // Also fetch top 3 comments by likes > recency
            if (posts == null)
            {
                throw new Exception("Cannot fetch post");
            }
            response.Success = true;
            response.Message = "Post fetched successfully";
            response.Data = posts;
            return Ok(response);
        }

        [HttpPost("updatepost")]
        public async Task<IActionResult> UpdatePost([FromBody] CreateUpdatePostRequestDto updatePostRequest)
        {
            CommonUtils.ControllerResponseParams response = new CommonUtils.ControllerResponseParams();
            if (await _rateLimiter.IsRequestAllowedAsync(AppConstants.ApiRequestAction.UpdatePost))
            {
                long userId = _userService.GetCurrentLoggedInUserId();
                Post? post = await _postService.UpdatePostAsync(updatePostRequest.PostId, userId, updatePostRequest.Content);
                if (post == null)
                {
                    throw new Exception("Cannot update post");
                }
                response.Success = true;
                response.Message = "Post updated successfully";
                response.Data = post;
            }
            return Ok(response);
        }

        [HttpPost("deletepostbyid")]
        public async Task<IActionResult> DeletePostById([FromBody] long postId)
        {
            CommonUtils.ControllerResponseParams response = new CommonUtils.ControllerResponseParams();
            long userId = _userService.GetCurrentLoggedInUserId();
            await _postService.DeletePostAsync(postId, userId);
            response.Success = true;
            response.Message = "Post deleted successfully";
            return Ok(response);
        }
    }
}
