using Microblog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Microblog.Api.Services
{
    public class PostService : IPostService
    {
        private readonly AppDbContext _dbContext;
        private readonly IDatabase _inMemoryDb;

        public PostService(AppDbContext dbContext, IConnectionMultiplexer connectionMultiplexer)
        {
            _inMemoryDb = connectionMultiplexer.GetDatabase();
            _dbContext = dbContext;
        }

        public async Task<Post> CreatePostAsync(long userId, string content)
        {
            if (userId == 0)
            {
                throw new Exception("Cannot create post");
            }
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new Exception("Cannot create empty post");
            }

            Post newPost = new Post
            {
                UserId = userId,
                Content = content
            };

            await _dbContext.Posts.AddAsync(newPost);
            await _dbContext.SaveChangesAsync();

            return newPost;
        }

        public async Task DeletePostAsync(long postId, long userId)
        {
            if (postId == 0)
            {
                throw new Exception("Unable to delete post. Post Id not provided");
            }

            Post? post = await _dbContext.Posts.FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId);
            if (post != null)
            {
                _dbContext.Posts.Remove(post);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<Post> GetPostByIdAsync(long postId)
        {
            Post? post = await _dbContext.Posts
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
            {
                throw new Exception("Cannot fetch post");
            }

            return post;
        }

        public async Task<List<Post>> GetPostsByIdListAsync(List<long> postIdList)
        {
            List<Post> posts = await _dbContext.Posts
                .Where(p => postIdList.Contains(p.Id))
                .ToListAsync();

            return posts;
        }

        public async Task<List<Post>> GetUserPostsAsync(long userId, int page = 1, int pageSize = 10, Order sortOrderByCreatedAt = Order.Descending)
        {
            return await _dbContext.Posts
                .Where(p => p.UserId == userId)
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Post> UpdatePostAsync(long postId, long userId, string content)
        {
            if (postId == 0)
            {
                throw new Exception("Cannot update post");
            }
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new Exception("Cannot create empty post");
            }

            Post? originalPostObj = await _dbContext.Posts.FirstOrDefaultAsync(_post => _post.Id == postId);

            if (originalPostObj == null)
            {
                Post? updatedPostObj = await CreatePostAsync(userId, content);

                if (updatedPostObj == null)
                {
                    throw new Exception("Cannot update post");
                }

                return updatedPostObj;
            }

            originalPostObj.Content = content;
            originalPostObj.ModifiedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            return originalPostObj;
        }
    }
}
