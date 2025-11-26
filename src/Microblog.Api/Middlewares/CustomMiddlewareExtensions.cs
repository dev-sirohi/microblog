namespace Microblog.Api.Middlewares
{
    public static class CustomMiddlewareExtensions
    {
        public static IApplicationBuilder UseTestMiddleware(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            app.UseMiddleware<TestMiddleware>();
            return app;
        }

        public static IApplicationBuilder UseExceptionHandlerMiddleware(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            app.UseMiddleware<ExceptionHandlerMiddleware>();
            return app;
        }
    }
}
