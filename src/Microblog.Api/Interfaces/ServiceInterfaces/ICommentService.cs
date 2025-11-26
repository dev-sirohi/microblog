namespace Microblog.Api.Interfaces.ServiceInterfaces
{
    public interface ICommentService
    {
        Task<Comment> AddCommentAsync(long userId, long postId, string content);
        Task<Comment> UpdateCommentAsync(long userId, long commentId, string content);
        Task<List<Comment>> GetCommentsByPostAsync(long postId);
        Task DeleteCommentAsync(long commentId, long userId);
    }
}
