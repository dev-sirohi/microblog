using System.Net.Http.Json;

namespace Microblog.LoadTests.Shared;

// One logged-in fake user. Its Client already sends the auth token on every request,
// so scenarios can just call endpoints through it.
public sealed class AuthedUser
{
    public HttpClient Client { get; }
    public string Username { get; }
    public long UserId { get; }

    public AuthedUser(HttpClient client, string username, long userId)
    {
        Client = client;
        Username = username;
        UserId = userId;
    }

    // Creates a post as this user and returns its id.
    // Handy for tests that need something to like / comment on / read.
    public async Task<long> CreatePostAsync(string content)
    {
        var resp = await Client.PostAsJsonAsync("/api/Post", new PostDto { Content = content });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PostData>>();
        return body!.Data!.Id;
    }
}
