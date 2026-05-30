using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Microblog.Api.Services;

public class AuthService(
    IConfiguration configuration,
    AppDbContext dbContext,
    IConnectionMultiplexer connectionMultiplexer)
    : IAuthService
{
    private readonly IDatabase _inMemoryDb = connectionMultiplexer.GetDatabase();

    /* Auth methods */
    public async Task<User> RegisterAsync(string username, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new Exception("Username cannot be empty");
        if (string.IsNullOrWhiteSpace(email)) throw new Exception("Email cannot be empty");
        if (string.IsNullOrWhiteSpace(password)) throw new Exception("Password cannot be empty");
        if (await dbContext.Users.AnyAsync(user => user.Email == email || user.Username == username))
            throw new Exception("User with the same email or username already exists");

        string passwordHash = CommonUtils.HashPassword(password);

        User newUser = new()
        {
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            IsEmailConfirmed = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        await dbContext.Users.AddAsync(newUser);
        await dbContext.SaveChangesAsync();

        return newUser;
    }

    public async Task<dynamic> LoginAsync(string username, string email, string password)
    {
        string accessToken = string.Empty;
        string refreshToken = string.Empty;
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(username))
            throw new Exception("Both Email and Username cannot be empty");
        if (string.IsNullOrWhiteSpace(password)) throw new Exception("Password cannot be empty");

        string passwordHash = CommonUtils.HashPassword(password);

        var user = await dbContext.Users.FirstOrDefaultAsync(user =>
            ((!string.IsNullOrWhiteSpace(username) &&
              user.Username.Equals(username, StringComparison.OrdinalIgnoreCase)) ||
             (!string.IsNullOrWhiteSpace(email) && user.Email.Equals(email, StringComparison.OrdinalIgnoreCase))) &&
            user.PasswordHash == passwordHash);
        if (user == null) throw new Exception("Invalid credentials");

        accessToken = GenerateAccessToken(user);
        var authToken = await GenerateRefreshTokenAsync(user.Id);
        refreshToken = authToken.RefreshToken;

        return new
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    public async Task Logout(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;

        var authToken = await dbContext.AuthTokens.SingleOrDefaultAsync(at => at.RefreshToken == refreshToken);
        if (authToken == null)
        {
            // Refresh token doesn't exist in memory so just blacklist it for a short duration
            await BlacklistTokenInMemory(refreshToken, TimeSpan.FromMinutes(5));
            return;
        }

        var expiry = authToken.RefreshTokenExpiry - DateTime.UtcNow;
        await BlacklistTokenInMemory(refreshToken, expiry);
        await DeleteToken(refreshToken);
    }

    /* Token methods */
    public async Task<AuthToken> RefreshAccessTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new AppException("Refresh token cannot be empty", HttpStatusCode.Unauthorized);
        if (await IsTokenBlacklisted(refreshToken))
            throw new AppException("Refresh token is blacklisted", HttpStatusCode.Unauthorized);

        var existing = await dbContext.AuthTokens.SingleOrDefaultAsync(a => a.RefreshToken == refreshToken);

        if (existing == null) throw new AppException("Invalid refresh token", HttpStatusCode.Unauthorized);

        var expiry = existing.RefreshTokenExpiry - DateTime.UtcNow;
        if (expiry > TimeSpan.Zero) await BlacklistTokenInMemory(refreshToken, expiry);

        dbContext.AuthTokens.RemoveRange(dbContext.AuthTokens.Where(t => t.UserId == existing.UserId));

        var newToken = await GenerateRefreshTokenAsync(existing.UserId);

        await dbContext.AuthTokens.AddAsync(newToken);
        await dbContext.SaveChangesAsync();

        return newToken;
    }


    public async Task<AuthToken> GenerateRefreshTokenAsync(long userId)
    {
        if (userId == 0) throw new Exception("Cannot generate refresh token. Invalid user Id");

        string refreshToken = string.Empty;
        byte[] randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            refreshToken = Convert.ToBase64String(randomNumber);
        }

        AuthToken authToken = new()
        {
            UserId = userId,
            RefreshToken = refreshToken,
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(7)
        };

        await dbContext.AuthTokens.AddAsync(authToken);
        await dbContext.SaveChangesAsync();

        return authToken;
    }

    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]
                                                                  ?? throw new Exception("Could not fetch Jwt key")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Convert.ToString(user.Id)),
            new(ClaimTypes.Name, user.Username)
        };

        var accessToken = new JwtSecurityToken(
            configuration["Jwt:Issuer"],
            configuration["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(configuration["Jwt:ExpiresMinutes"])),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(accessToken);
    }

    private async Task DeleteToken(string token)
    {
        var authToken = await dbContext.AuthTokens.SingleOrDefaultAsync(authToken => authToken.RefreshToken == token);
        if (authToken == null) return;

        dbContext.AuthTokens.RemoveRange(
            dbContext.AuthTokens.Where(_authToken => _authToken.UserId == authToken.UserId));
        await dbContext.SaveChangesAsync();
    }

    private async Task BlacklistTokenInMemory(string token, TimeSpan expiry)
    {
        await _inMemoryDb.StringSetAsync(token, "blacklisted", expiry);
    }

    private async Task<bool> IsTokenBlacklisted(string token)
    {
        var value = await _inMemoryDb.StringGetAsync(token);
        return value.HasValue;
    }
}
