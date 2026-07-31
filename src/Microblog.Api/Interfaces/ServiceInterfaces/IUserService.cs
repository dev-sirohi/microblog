namespace Microblog.Api.Interfaces.ServiceInterfaces;

public interface IUserService
{
    long GetCurrentLoggedInUserId();
    Task<User> GetCurrentLoggedInUserAsync();
    Task<User> GetUserByIdAsync(long userId);
    Task<IReadOnlyCollection<User>> GetUserListByIdListReadOnlyAsync(IReadOnlyCollection<long> userIdList);
}