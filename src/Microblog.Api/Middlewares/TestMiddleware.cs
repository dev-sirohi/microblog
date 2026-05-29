namespace Microblog.Api.Middlewares;

public class TestMiddleware
{
    private readonly RequestDelegate _next;

    public TestMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Middleware logic goes here
        // Call the next middleware in the pipeline
        await _next(context);
    }
}