using System.Text.Json;
using IdentityAuth.Application.Authentication.Services;
using Microsoft.AspNetCore.Mvc;

using InvalidRefreshTokenException = IdentityAuth.Application.Authentication.Services.InvalidRefreshTokenException;
using RefreshTokenReuseException = IdentityAuth.Application.Authentication.Services.RefreshTokenReuseException;

namespace IdentityAuth.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var problemDetails = exception switch
        {
            DuplicateEmailException dupEx => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = dupEx.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10"
            },
            AuthenticationException authEx => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = authEx.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
            },
            AccountLockedException lockEx => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = lockEx.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4"
            },
            EmailNotVerifiedException emailEx => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = emailEx.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4"
            },
            InvalidRefreshTokenException refreshEx => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = refreshEx.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
            },
            RefreshTokenReuseException reuseEx => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = reuseEx.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
            },
            ArgumentException argEx => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = argEx.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
            },
            UnauthorizedAccessException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "You are not authorized to perform this action.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
            },
            KeyNotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = "The requested resource was not found.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5"
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred. Please try again later.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
            }
        };

        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, jsonOptions));
    }
}
