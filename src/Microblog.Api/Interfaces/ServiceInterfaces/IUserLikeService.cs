namespace Microblog.Api.Interfaces.ServiceInterfaces
{
    public interface IUserLikeService
    {
        Task LikePostAsync(long userId, long postId, bool useCache = true);
        Task UnlikePostAsync(long userId, long postId, bool useCache = true);
        Task<(long likesCount, bool isLikedByUser)> GetPostLikesAndIsLikedByUserAsync(long userId, long postId, bool useCache = true);
        Task<List<long>> GetRecentlyLikedPostIdsByUserAsync(long userId, int page = 1, int limit = 10, bool useCache = true);
        Task<List<Post>> GetRecentlyLikedPostsByUserAsync(long userId, int page = 1, int limit = 10, bool useCache = true);
    }
}
