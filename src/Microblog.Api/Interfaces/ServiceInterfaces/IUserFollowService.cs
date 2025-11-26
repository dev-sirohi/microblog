namespace Microblog.Api.Interfaces.ServiceInterfaces
{
    public interface IUserFollowService
    {
        Task FollowUserAsync(long followerId, long followingId);
        Task UnfollowUserAsync(long followerId, long followingId);
        Task<IReadOnlyCollection<long>> GetFollowingIdListAsync(long userId);
        Task<IReadOnlyCollection<long>> GetFollowerIdListAsync(long userId);
    }
}
