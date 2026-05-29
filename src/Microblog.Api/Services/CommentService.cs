namespace Microblog.Api.Services;

public class CommentService(AppDbContext dbContext, IConnectionMultiplexer connectionMultiplexer)
    : ICommentService
{
    private readonly IDatabase _inMemoryDb = connectionMultiplexer.GetDatabase();

    public async Task<Comment> AddCommentAsync(long userId, long postId, string content)
    {
        if (userId == 0 || postId == 0) throw new Exception("Cannot add comment");
        if (string.IsNullOrWhiteSpace(content)) throw new Exception("Cannot add empty comment");

        var comment = new Comment
        {
            UserId = userId,
            PostId = postId,
            Content = content
        };

        await dbContext.Comments.AddAsync(comment);
        await dbContext.SaveChangesAsync();

        return comment;
    }

    public async Task<Comment> UpdateCommentAsync(long userId, long commentId, string content)
    {
        if (userId == 0 || commentId == 0) throw new Exception("Cannot update comment");
        if (string.IsNullOrWhiteSpace(content)) throw new Exception("Cannot update to empty comment");

        var comment = await dbContext.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userId);
        if (comment == null) throw new Exception("Cannot update comment");

        comment.Content = content;
        comment.UpdatedAt = DateTime.UtcNow;

        await dbContext.Comments.AddAsync(comment);
        await dbContext.SaveChangesAsync();

        return comment;
    }

    public async Task<List<Comment>> GetCommentsByPostAsync(long postId)
    {
        return await (
            from c in dbContext.Comments
            join u in dbContext.Users
                on c.UserId equals u.Id
            where c.PostId == postId
            orderby c.CreatedAt
            select new Comment
            {
                Id = c.Id,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                PostId = c.PostId,
                UserId = c.UserId,
                User = new User
                {
                    Id = u.Id,
                    Username = u.Username,
                }
            }
        ).ToListAsync();
    }

    public async Task DeleteCommentAsync(long commentId, long userId)
    {
        var comment = await dbContext.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userId);
        if (comment != null)
        {
            dbContext.Comments.Remove(comment);
            await dbContext.SaveChangesAsync();
        }
    }
}