namespace Microblog.Api.Interfaces.ServiceInterfaces;

public interface IAuthService
{
    Task<User> RegisterAsync(string username, string email, string password);
    Task<dynamic> LoginAsync(string username, string email, string password);
    Task<AuthToken> GenerateRefreshTokenAsync(long userId);
    Task<AuthToken> RefreshAccessTokenAsync(string token);
    Task Logout(string refreshToken);
}