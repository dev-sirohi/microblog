using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Microblog.Tests;

[Collection("api")]
public class RateLimitingTests(MicroblogApiFactory factory)
{
    [Fact]
    public async Task Login_Endpoint_Returns_429_After_Exceeding_The_Sliding_Window()
    {
        // The login policy allows 5 requests/minute per caller. A 6th within the window is rejected.
        var client = TestHelpers.NewClient(factory, "203.0.113.10");

        var payload = new { Username = "nobody", Email = "", Password = "whatever" };

        HttpStatusCode? sixth = null;
        for (int i = 0; i < 6; i++)
        {
            var res = await client.PostAsJsonAsync("/api/auth/login", payload);
            if (i == 5) sixth = res.StatusCode;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, sixth);
    }

    [Fact]
    public async Task Distinct_Policies_Are_Independent()
    {
        // Exhausting the login limit must not block registration (separate per-endpoint policy).
        var client = TestHelpers.NewClient(factory, "203.0.113.20");
        var login = new { Username = "someone", Email = "", Password = "whatever" };
        for (int i = 0; i < 6; i++)
            await client.PostAsJsonAsync("/api/auth/login", login);

        string username = $"rl_{Guid.NewGuid():N}".Substring(0, 14);
        var reg = await client.PostAsJsonAsync("/api/auth/register",
            new { Username = username, Email = $"{username}@t.dev", Password = "P@ssw0rd123" });

        Assert.NotEqual(HttpStatusCode.TooManyRequests, reg.StatusCode);
    }
}
