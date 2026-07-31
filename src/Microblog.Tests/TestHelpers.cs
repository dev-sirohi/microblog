using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Microblog.Tests;

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<MicroblogApiFactory> { }

internal static class TestHelpers
{
    private static int _seq;

    public static HttpClient NewClient(MicroblogApiFactory factory, string? ip = null)
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Test-Client-Ip", ip ?? RandomIp());
        return client;
    }

    public static string RandomIp()
    {
        var r = Random.Shared;
        return $"{r.Next(11, 250)}.{r.Next(0, 255)}.{r.Next(0, 255)}.{r.Next(1, 254)}";
    }

    public static async Task<(HttpClient client, string username)> RegisterAndLoginAsync(MicroblogApiFactory factory)
    {
        var client = NewClient(factory);

        string username = $"user{Interlocked.Increment(ref _seq)}_{Guid.NewGuid():N}".Substring(0, 20);
        string password = "P@ssw0rd123";
        string email = $"{username}@test.dev";

        var reg = await client.PostAsJsonAsync("/api/auth/register",
            new { Username = username, Email = email, Password = password });
        reg.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { Username = username, Email = email, Password = password });
        login.EnsureSuccessStatusCode();

        string accessToken = ExtractCookie(login, "accessToken")
                             ?? throw new Exception("login did not set an accessToken cookie");
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", $"accessToken={accessToken}");

        return (client, username);
    }

    public static string? ExtractCookie(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies)) return null;
        foreach (var c in cookies)
        {
            var first = c.Split(';')[0];
            var idx = first.IndexOf('=');
            if (idx > 0 && first[..idx].Trim() == name)
                return first[(idx + 1)..].Trim();
        }
        return null;
    }

    public static async Task<JsonElement> ReadDataAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.TryGetProperty("Data", out var data) ? data : default;
    }
}
