using System.Security.Claims;

namespace Microblog.Api.Services;

public class UserService(AppDbContext dbContext, IHttpContextAccessor httpContext)
{
    public long GetCurrentLoggedInUserId()
    {
        var context = httpContext.HttpContext;
        if (context?.User?.Identity?.IsAuthenticated != true)
            throw new AppException("User logged out", HttpStatusCode.Unauthorized);

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out long userId) || userId == 0)
            throw new AppException("User logged out", HttpStatusCode.Unauthorized);

        return userId;
    }

    public async Task<User> GetCurrentLoggedInUserAsync()
    {
        long userId = GetCurrentLoggedInUserId();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        return user ?? throw new AppException("Cannot fetch user", HttpStatusCode.NotFound);
    }

    public async Task<User> GetUserByIdAsync(long userId)
    {
        if (userId == 0) throw new AppException("User not found", HttpStatusCode.BadRequest);

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        return user ?? throw new AppException("User not found", HttpStatusCode.NotFound);
    }

    public async Task<IReadOnlyCollection<User>> GetUserListByIdListAsync(IReadOnlyCollection<long> userIdList)
    {
        if (userIdList.Count == 0) return [];

        return await dbContext.Users.Where(user => userIdList.Contains(user.Id)).ToListAsync();
    }

    public async Task SetProfilePictureUrlAsync(long userId, string url)
    {
        var user = await GetUserByIdAsync(userId);
        user.ProfilePictureUrl = url;
        user.ModifiedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }
}
