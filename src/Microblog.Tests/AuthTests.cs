using System.Net;
using System.Net.Http.Json;
using Microblog.Api.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microblog.Tests;

[Collection("api")]
public class AuthTests(MicroblogApiFactory factory)
{
    [Fact]
    public async Task Register_Login_SetsCookies_And_PersistsUser()
    {
        var client = TestHelpers.NewClient(factory);

        string username = $"authuser_{Guid.NewGuid():N}".Substring(0, 18);
        string email = $"{username}@test.dev";

        var reg = await client.PostAsJsonAsync("/api/auth/register",
            new { Username = username, Email = email, Password = "P@ssw0rd123" });
        Assert.Equal(HttpStatusCode.OK, reg.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { Username = username, Email = "", Password = "P@ssw0rd123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        // Cookie-based session: both tokens come back as cookies.
        Assert.NotNull(TestHelpers.ExtractCookie(login, "accessToken"));
        Assert.NotNull(TestHelpers.ExtractCookie(login, "refreshToken"));

        // EF Core persistence: the user row exists in SQL Server.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.Users.AnyAsync(u => u.Username == username));
    }

    [Fact]
    public async Task Me_Requires_Auth_And_Returns_Current_User()
    {
        var anon = factory.CreateClient();
        var unauth = await anon.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);

        var (client, username) = await TestHelpers.RegisterAndLoginAsync(factory);
        var me = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var data = await TestHelpers.ReadDataAsync(me);
        Assert.Equal(username, data.GetProperty("Username").GetString());
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Does_Not_Set_Cookie()
    {
        var client = TestHelpers.NewClient(factory);
        string username = $"wrongpw_{Guid.NewGuid():N}".Substring(0, 16);
        await client.PostAsJsonAsync("/api/auth/register",
            new { Username = username, Email = $"{username}@t.dev", Password = "P@ssw0rd123" });

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { Username = username, Email = "", Password = "wrong-password" });

        Assert.Null(TestHelpers.ExtractCookie(login, "accessToken"));
    }
}
