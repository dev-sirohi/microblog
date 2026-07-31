namespace Microblog.LoadTests.Shared;

// Private copies of the request/response shapes we need.
// We keep our own copies (instead of referencing Microblog.Api) so this folder
// stays portable — it can be moved out and pointed at any compatible API.

// ---- Requests we send ----

public sealed class RegisterDto
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class LoginDto
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class PostDto
{
    public string Content { get; set; } = "";
}

// ---- Responses we read ----

// Every endpoint wraps its result in { Success, Message, Data }.
public sealed class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

// We only need the Id out of the user/post objects the API returns.
public sealed class UserData { public long Id { get; set; } }
public sealed class PostData { public long Id { get; set; } }
