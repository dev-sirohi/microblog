namespace Microblog.Api.Interfaces.ServiceInterfaces;

public interface IPostService
{
    Task<Post> CreatePostAsync(long userId, string content);
    Task<Post> GetPostByIdAsync(long postId);

    Task<List<Post>> GetUserPostsAsync(long userId, int page = 1, int pageSize = 10,
        Order sortOrderByCreatedAt = Order.Descending);

    Task<Post> UpdatePostAsync(long postId, long userId, string content);
    Task DeletePostAsync(long postId, long userId);
    Task<List<Post>> GetPostsByIdListAsync(List<long> postIdList);
}