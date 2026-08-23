using System.Text.RegularExpressions;

namespace IdentityAuth.Api.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private const int MaxCorrelationIdLength = 128;
    private readonly RequestDelegate _next;

    // Only allow alphanumeric, hyphens, underscores, and dots (safe for logs and headers)
    private static readonly Regex ValidCorrelationIdPattern = new(@"^[a-zA-Z0-9\-_\.]+$", RegexOptions.Compiled);

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Use existing correlation ID from request headers if valid, or generate a new one
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(correlationId)
            || correlationId.Length > MaxCorrelationIdLength
            || !ValidCorrelationIdPattern.IsMatch(correlationId))
        {
            // Reject invalid client-supplied IDs to prevent log injection
            correlationId = Guid.NewGuid().ToString("N");
        }

        // Store in HttpContext.Items for access throughout the request pipeline
        context.Items["CorrelationId"] = correlationId;

        // Add correlation ID to response headers for tracing
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
