namespace Microblog.Api.Services;

public class PostService(AppDbContext dbContext, IConnectionMultiplexer connectionMultiplexer)
    : IPostService
{
    private readonly IDatabase _inMemoryDb = connectionMultiplexer.GetDatabase();

    public async Task<Post> CreatePostAsync(long userId, string content)
    {
        if (userId == 0) throw new Exception("Cannot create post");
        if (string.IsNullOrWhiteSpace(content)) throw new Exception("Cannot create empty post");

        var newPost = new Post
        {
            UserId = userId,
            Content = content
        };

        await dbContext.Posts.AddAsync(newPost);
        await dbContext.SaveChangesAsync();

        return newPost;
    }

    public async Task DeletePostAsync(long postId, long userId)
    {
        if (postId == 0) throw new Exception("Unable to delete post. Post Id not provided");

        var post = await dbContext.Posts.FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId);
        if (post != null)
        {
            dbContext.Posts.Remove(post);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<Post> GetPostByIdAsync(long postId)
    {
        var post = await (
            from p in dbContext.Posts
            where p.Id == postId
            select new Post
            {
                Id = p.Id,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                UserId = p.UserId,
            }).FirstOrDefaultAsync();

        return post ?? throw new Exception("Cannot fetch post");
    }

    public async Task<List<Post>> GetPostsByIdListAsync(List<long> postIdList)
    {
        var posts = await dbContext.Posts
            .Where(p => postIdList.Contains(p.Id))
            .ToListAsync();

        return posts;
    }

    public async Task<List<Post>> GetUserPostsAsync(long userId, int page = 1, int pageSize = 10,
        Order sortOrderByCreatedAt = Order.Descending)
    {
        var posts = await dbContext.Posts
            .Where(p => p.UserId == userId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        if (sortOrderByCreatedAt == Order.Descending)
        {
            posts = posts.OrderByDescending(p => p.CreatedAt).ToList();
        }
        else
        {
            posts = posts.OrderBy(p => p.CreatedAt).ToList();
        }
        return posts;
    }

    public async Task<Post> UpdatePostAsync(long postId, long userId, string content)
    {
        if (postId == 0) throw new Exception("Cannot update post");
        if (string.IsNullOrWhiteSpace(content)) throw new Exception("Cannot create empty post");

        var originalPostObj = await dbContext.Posts.FirstOrDefaultAsync(_post => _post.Id == postId);

        if (originalPostObj == null)
        {
            var updatedPostObj = await CreatePostAsync(userId, content);

            return updatedPostObj ?? throw new Exception("Cannot update post");
        }

        originalPostObj.Content = content;
        originalPostObj.ModifiedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return originalPostObj;
    }
}