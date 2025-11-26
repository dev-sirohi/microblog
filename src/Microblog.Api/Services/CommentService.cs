using Microblog.Api.Models;

namespace Microblog.Api.Services
{
    public class CommentService : ICommentService
    {
        private readonly AppDbContext _dbContext;
        private readonly IDatabase _inMemoryDb;

        public CommentService(AppDbContext dbContext, IConnectionMultiplexer connectionMultiplexer)
        {
            _dbContext = dbContext;
            _inMemoryDb = connectionMultiplexer.GetDatabase();
        }

        public async Task<Comment> AddCommentAsync(long userId, long postId, string content)
        {
            if (userId == 0 || postId == 0)
            {
                throw new Exception("Cannot add comment");
            }
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new Exception("Cannot add empty comment");
            }

            Comment comment = new Comment
            {
                UserId = userId,
                PostId = postId,
                Content = content
            };

            await _dbContext.Comments.AddAsync(comment);
            await _dbContext.SaveChangesAsync();

            return comment;
        }

        public async Task<Comment> UpdateCommentAsync(long userId, long commentId, string content)
        {
            if (userId == 0 || commentId == 0)
            {
                throw new Exception("Cannot update comment");
            }
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new Exception("Cannot update to empty comment");
            }

            Comment? comment = await _dbContext.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userId);
            if (comment == null)
            {
                throw new Exception("Cannot update comment");
            }

            comment.Content = content;
            comment.UpdatedAt = DateTime.UtcNow;

            await _dbContext.Comments.AddAsync(comment);
            await _dbContext.SaveChangesAsync();

            return comment;
        }

        public async Task<List<Comment>> GetCommentsByPostAsync(long postId)
        {
            return await _dbContext.Comments
                .Where(c => c.PostId == postId)
                .Include(c => c.User)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task DeleteCommentAsync(long commentId, long userId)
        {
            Comment? comment = await _dbContext.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userId);
            if (comment != null)
            {
                _dbContext.Comments.Remove(comment);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
