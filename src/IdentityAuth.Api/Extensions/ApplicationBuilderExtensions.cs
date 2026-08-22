using IdentityAuth.Api.Middleware;

namespace IdentityAuth.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        return app;
    }

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-XSS-Protection", "0");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

            await next();
        });

        return app;
    }

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        return app;
    }

    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestLoggingMiddleware>();
        return app;
    }
}
