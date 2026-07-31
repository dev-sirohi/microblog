using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Microblog.LoadTests.Shared;

// Knows how to create a brand-new fake user and log them in.
// The result is an AuthedUser: an HttpClient that already carries the login token.
public static class AuthHelper
{
    public static async Task<AuthedUser> RegisterAndLoginAsync()
    {
        // Unique identity per fake user so registrations never collide.
        var id = Guid.NewGuid().ToString("N");
        var username = $"lt_{id}";
        var email = $"{id}@loadtest.local";
        const string password = "TestPassword123!";

        // A plain client just for the register + login handshake.
        using var setup = NewClient();

        // 1) Register the account.
        var regResp = await setup.PostAsJsonAsync("/api/Auth/register",
            new RegisterDto { Username = username, Email = email, Password = password });
        regResp.EnsureSuccessStatusCode();
        var reg = await regResp.Content.ReadFromJsonAsync<ApiResponse<UserData>>();
        long userId = reg!.Data!.Id;

        // 2) Log in. The API returns the token as a Set-Cookie header (accessToken=...).
        var loginResp = await setup.PostAsJsonAsync("/api/Auth/login",
            new LoginDto { Username = username, Email = email, Password = password });
        loginResp.EnsureSuccessStatusCode();
        string token = ExtractAccessToken(loginResp);

        // 3) Build the real client this user will use. The API's JWT check reads the
        //    Authorization header, so we forward the cookie's token value there.
        var client = NewClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return new AuthedUser(client, username, userId);
    }

    // UseCookies=false so the token never gets auto-stored — we read Set-Cookie ourselves.
    private static HttpClient NewClient() =>
        new(new HttpClientHandler { UseCookies = false }) { BaseAddress = new Uri(Config.BaseUrl) };

    // Find "accessToken=...." inside the Set-Cookie header(s) and return just the value.
    private static string ExtractAccessToken(HttpResponseMessage resp)
    {
        if (resp.Headers.TryGetValues("Set-Cookie", out var cookies))
            foreach (var cookie in cookies)
                if (cookie.StartsWith("accessToken=", StringComparison.OrdinalIgnoreCase))
                    return cookie["accessToken=".Length..].Split(';')[0];

        throw new Exception("Login succeeded but no accessToken cookie was returned.");
    }
}
