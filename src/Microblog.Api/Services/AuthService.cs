using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Microblog.Api.Services;

public class AuthService(IConfiguration configuration, AppDbContext dbContext)
{
    public async Task<User> RegisterAsync(string username, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new AppException("Username cannot be empty", HttpStatusCode.BadRequest);
        if (string.IsNullOrWhiteSpace(email)) throw new AppException("Email cannot be empty", HttpStatusCode.BadRequest);
        if (string.IsNullOrWhiteSpace(password)) throw new AppException("Password cannot be empty", HttpStatusCode.BadRequest);
        if (await dbContext.Users.AnyAsync(user => user.Email == email || user.Username == username))
            throw new AppException("User with the same email or username already exists", HttpStatusCode.Conflict);

        User newUser = new()
        {
            Username = username,
            Email = email,
            PasswordHash = CommonUtils.HashPassword(password),
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
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(username))
            throw new AppException("Both Email and Username cannot be empty", HttpStatusCode.BadRequest);
        if (string.IsNullOrWhiteSpace(password)) throw new AppException("Password cannot be empty", HttpStatusCode.BadRequest);

        string passwordHash = CommonUtils.HashPassword(password);

        bool hasUsername = !string.IsNullOrWhiteSpace(username);
        bool hasEmail = !string.IsNullOrWhiteSpace(email);

        var user = await dbContext.Users.FirstOrDefaultAsync(u =>
            ((hasUsername && u.Username == username) || (hasEmail && u.Email == email))
            && u.PasswordHash == passwordHash);
        if (user == null) throw new AppException("Invalid credentials", HttpStatusCode.Unauthorized);

        string accessToken = GenerateAccessToken(user);
        var authToken = await GenerateRefreshTokenAsync(user.Id);

        return new
        {
            AccessToken = accessToken,
            RefreshToken = authToken.RefreshToken,
            User = user
        };
    }

    public async Task Logout(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;

        var authToken = await dbContext.AuthTokens.SingleOrDefaultAsync(at => at.RefreshToken == refreshToken);
        if (authToken == null) return;

        dbContext.AuthTokens.RemoveRange(dbContext.AuthTokens.Where(t => t.UserId == authToken.UserId));
        await dbContext.SaveChangesAsync();
    }

    public async Task<AuthToken> RefreshAccessTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new AppException("Refresh token cannot be empty", HttpStatusCode.Unauthorized);

        var existing = await dbContext.AuthTokens.SingleOrDefaultAsync(a => a.RefreshToken == refreshToken);
        if (existing == null) throw new AppException("Invalid refresh token", HttpStatusCode.Unauthorized);
        if (existing.RefreshTokenExpiry <= DateTime.UtcNow)
            throw new AppException("Refresh token has expired", HttpStatusCode.Unauthorized);

        dbContext.AuthTokens.RemoveRange(dbContext.AuthTokens.Where(t => t.UserId == existing.UserId));
        await dbContext.SaveChangesAsync();

        return await GenerateRefreshTokenAsync(existing.UserId);
    }

    public async Task<AuthToken> GenerateRefreshTokenAsync(long userId)
    {
        if (userId == 0) throw new AppException("Cannot generate refresh token. Invalid user Id", HttpStatusCode.BadRequest);

        byte[] randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        AuthToken authToken = new()
        {
            UserId = userId,
            RefreshToken = Convert.ToBase64String(randomNumber),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(
                Convert.ToDouble(configuration["Jwt:RefreshTokenExpireDays"]))
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
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(configuration["Jwt:ExpireMinutes"])),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(accessToken);
    }
}
