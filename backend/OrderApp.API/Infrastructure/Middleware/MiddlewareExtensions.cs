namespace OrderApp.API.Infrastructure.Middleware;

public static class MiddlewareExtensions
{
    /// <summary>
    /// Program.cs'de tek satırla kayıt: app.UseGlobalExceptionHandler();
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}
