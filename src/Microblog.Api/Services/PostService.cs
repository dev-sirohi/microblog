using Microblog.Api.Features.Recommendations;
using Microblog.Api.Infrastructure.Messaging;
using Microblog.Api.Utils;

namespace Microblog.Api.Services;

public class PostService(
    AppDbContext dbContext,
    IConnectionMultiplexer connectionMultiplexer,
    IServiceProvider serviceProvider,
    GlobalConfig globalConfig) : IPostService
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

        // Fire-and-forget: publish event + queue embedding computation
        _ = Task.Run(async () =>
        {
            await TryPublishEventAsync(new PostCreatedEvent(newPost.Id, userId, content, newPost.CreatedAt));
            await TryQueueEmbeddingAsync(newPost.Id, content);
        });

        return newPost;
    }

    public async Task<List<Post>> GetHomeFeedAsync(int page = 1, int pageSize = 10)
    {
        if (page < 1) page = 1;
        pageSize = Math.Clamp(pageSize, 1, 50);

        // Simple reverse-chronological global feed.
        return await dbContext.Posts
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new Post
            {
                Id = p.Id,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                UserId = p.UserId
            })
            .ToListAsync();
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

        // Re-queue embedding when content changes
        _ = Task.Run(() => TryQueueEmbeddingAsync(postId, content));

        await dbContext.SaveChangesAsync();

        return originalPostObj;
    }

    private async Task TryPublishEventAsync(PostCreatedEvent evt)
    {
        try
        {
            var publisher = serviceProvider.GetService<IMessagePublisher>();
            if (publisher is not null)
                await publisher.PublishAsync("post.created", evt);
        }
        catch { /* messaging is best-effort */ }
    }

    private async Task TryQueueEmbeddingAsync(long postId, string content)
    {
        if (!globalConfig.EnableEmbeddings) return;
        try
        {
            using var scope = serviceProvider.CreateScope();
            var recommendations = scope.ServiceProvider.GetService<IRecommendationService>();
            if (recommendations is not null)
                await recommendations.ComputeAndStoreEmbeddingAsync(postId, content);
        }
        catch { /* embedding is non-critical */ }
    }
}
