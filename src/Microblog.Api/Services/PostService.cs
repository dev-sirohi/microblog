using Microblog.Api.Infrastructure.Caching;
using Microblog.Api.Infrastructure.Messaging;

namespace Microblog.Api.Services;

public class PostService(AppDbContext dbContext, ICacheService cacheService, IServiceProvider serviceProvider)
{
    private static string PostCacheKey(long postId) => $"post:{postId}";

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

        _ = Task.Run(() =>
            TryPublishEventAsync(new PostCreatedEvent(newPost.Id, userId, content, newPost.CreatedAt)));

        return newPost;
    }

    public async Task<List<Post>> GetHomeFeedAsync(int page = 1, int pageSize = 10)
    {
        if (page < 1) page = 1;
        pageSize = Math.Clamp(pageSize, 1, 50);

        return await dbContext.Posts
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Post> GetPostByIdAsync(long postId)
    {
        var post = await cacheService.GetOrSetAsync(
            PostCacheKey(postId),
            async () => await dbContext.Posts.FirstOrDefaultAsync(p => p.Id == postId),
            TimeSpan.FromMinutes(5));

        return post ?? throw new Exception("Cannot fetch post");
    }

    public async Task<List<Post>> GetPostsByIdListAsync(List<long> postIdList)
    {
        return await dbContext.Posts.Where(p => postIdList.Contains(p.Id)).ToListAsync();
    }

    public async Task<List<Post>> GetUserPostsAsync(long userId, int page = 1, int pageSize = 10)
    {
        return await dbContext.Posts
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Post> UpdatePostAsync(long postId, long userId, string content)
    {
        if (postId == 0) throw new Exception("Cannot update post");
        if (string.IsNullOrWhiteSpace(content)) throw new Exception("Cannot create empty post");

        var post = await dbContext.Posts.FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId);
        if (post == null) throw new Exception("Cannot update post");

        post.Content = content;
        post.ModifiedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        await cacheService.RemoveAsync(PostCacheKey(postId));

        return post;
    }

    public async Task DeletePostAsync(long postId, long userId)
    {
        if (postId == 0) throw new Exception("Unable to delete post. Post Id not provided");

        var post = await dbContext.Posts.FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId);
        if (post == null) return;

        dbContext.Posts.Remove(post);
        await dbContext.SaveChangesAsync();
        await cacheService.RemoveAsync(PostCacheKey(postId));
    }

    private async Task TryPublishEventAsync(PostCreatedEvent evt)
    {
        try
        {
            var publisher = serviceProvider.GetService<IMessagePublisher>();
            if (publisher is not null)
                await publisher.PublishAsync("post.created", evt);
        }
        catch { }
    }
}
