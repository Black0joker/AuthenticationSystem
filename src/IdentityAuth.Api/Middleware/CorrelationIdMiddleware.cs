namespace IdentityAuth.Api.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Use existing correlation ID from request headers, or generate a new one
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                         ?? Guid.NewGuid().ToString("N");

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
