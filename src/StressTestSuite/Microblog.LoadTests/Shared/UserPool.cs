namespace Microblog.LoadTests.Shared;

// A batch of logged-in fake users created up front (during a scenario's Init step).
// Scenarios pull users from here so the load looks like many different people,
// not one account hammering the API.
public sealed class UserPool
{
    private readonly AuthedUser[] _users;
    private int _cursor = -1;

    private UserPool(AuthedUser[] users) => _users = users;

    // Create `count` users, logging them all in (in parallel, so setup is fast).
    public static async Task<UserPool> CreateAsync(int count)
    {
        var tasks = Enumerable.Range(0, count).Select(_ => AuthHelper.RegisterAndLoginAsync());
        var users = await Task.WhenAll(tasks);
        return new UserPool(users);
    }

    public AuthedUser First() => _users[0];

    // Hand out users one after another, looping back to the start.
    // Thread-safe because NBomber calls this from many virtual users at once.
    public AuthedUser Next()
    {
        int i = Interlocked.Increment(ref _cursor) & int.MaxValue;
        return _users[i % _users.Length];
    }
}
