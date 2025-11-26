using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;

namespace Microblog.Api.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _dbContext;
        private readonly IDatabase _inMemoryDb;
        private readonly IHttpContextAccessor _httpContext;
        public UserService(AppDbContext dbContext, IConnectionMultiplexer connectionMultiplexer, IHttpContextAccessor httpContext)
        {
            _dbContext = dbContext;
            _inMemoryDb = connectionMultiplexer.GetDatabase();
            _httpContext = httpContext;
        }

        public long GetCurrentLoggedInUserId()
        {
            long userId = 0;
            var context = _httpContext.HttpContext;
            if (context?.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long uid))
                {
                    userId = uid;
                }
            }
            if (Convert.ToInt64(userId) == 0)
            {
                throw new AppException("User logged out", HttpStatusCode.Unauthorized);
            }
            return userId;
        }
        public async Task<User> GetCurrentLoggedInUserAsync()
        {
            long userId = GetCurrentLoggedInUserId();
            User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new Exception("Cannot fetch user");
            }

            return user;
        }

        public async Task<User> GetUserByIdAsync(long userId)
        {
            if (userId == 0)
            {
                throw new Exception("User not found");
            }

            User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            return user;
        }

        public async Task<IReadOnlyCollection<User>> GetUserListByIdListReadOnlyAsync(IReadOnlyCollection<long> userIdList)
        {
            if (userIdList.Count == 0)
            {
                throw new Exception("Cannot fetch users");
            }
            List<User> userList = await _dbContext.Users.Where(user => userIdList.Contains(user.Id)).ToListAsync();

            return userList;
        }

        public async Task<long> GetUserFollowerCountAsync(long userId)
        {
            return Convert.ToInt64(await _inMemoryDb.StringGetAsync(InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWER_COUNT)));
        }
        
        public async Task<long> GetUserFollowingCountAsync(long userId)
        {
            return Convert.ToInt64(await _inMemoryDb.StringGetAsync(InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.USER_FOLLOWING_COUNT)));
        }
    }
}
