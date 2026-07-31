using System.Security.Claims;

namespace Microblog.Api.Services;

public class UserService(
    AppDbContext dbContext,
    IConnectionMultiplexer connectionMultiplexer,
    IHttpContextAccessor httpContext)
    : IUserService
{
    private readonly IDatabase _inMemoryDb = connectionMultiplexer.GetDatabase();

    public long GetCurrentLoggedInUserId()
    {
        long userId = 0;
        var context = httpContext.HttpContext;
        if (context?.User?.Identity?.IsAuthenticated != true)
            return Convert.ToInt64(userId) == 0
                ? throw new AppException("User logged out", HttpStatusCode.Unauthorized)
                : userId;
        
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long uid)) userId = uid;

        return Convert.ToInt64(userId) == 0 ? throw new AppException("User logged out", HttpStatusCode.Unauthorized) : userId;
    }

    public async Task<User> GetCurrentLoggedInUserAsync()
    {
        long userId = GetCurrentLoggedInUserId();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        return user ?? throw new Exception("Cannot fetch user");
    }

    public async Task<User> GetUserByIdAsync(long userId)
    {
        if (userId == 0) throw new Exception("User not found");

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        return user ?? throw new Exception("User not found");
    }

    public async Task<IReadOnlyCollection<User>> GetUserListByIdListReadOnlyAsync(IReadOnlyCollection<long> userIdList)
    {
        if (userIdList.Count == 0) throw new Exception("Cannot fetch users");
        var userList = await dbContext.Users.Where(user => userIdList.Contains(user.Id)).ToListAsync();

        return userList;
    }
}