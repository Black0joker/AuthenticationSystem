using System.Text;
using System.Threading.RateLimiting;
using IdentityAuth.Api.Extensions;
using IdentityAuth.Api.HealthChecks;
using IdentityAuth.Application;
using IdentityAuth.Application.Common.Settings;
using IdentityAuth.Infrastructure;
using IdentityAuth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure OpenAPI/Swagger
builder.Services.AddOpenApi();

// Configure Problem Details for standardized error responses
builder.Services.AddProblemDetails();

// Register application layer services
builder.Services.AddApplication();

// Register infrastructure layer services (EF Core, Database)
builder.Services.AddInfrastructure(builder.Configuration);

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" });

// Configure JWT Authentication
var jwtSettings = new JwtSettings();
builder.Configuration.Bind(JwtSettings.SectionName, jwtSettings);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";
            return context.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = 401,
                    title = "Unauthorized",
                    detail = "You must be authenticated to access this resource.",
                    type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
                }, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                }));
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";
            return context.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = 403,
                    title = "Forbidden",
                    detail = "You do not have permission to access this resource.",
                    type = "https://tools.ietf.org/html/rfc9110#section-15.5.4"
                }, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                }));
        }
    };
});

// Configure Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
});

// Configure Rate Limiting (disabled in Development/Test to allow integration tests)
var enableRateLimiting = builder.Configuration.GetValue<bool>("RateLimiting:Enabled", true);

if (enableRateLimiting && !builder.Environment.IsDevelopment())
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType = "application/problem+json";

            var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                ? retryAfterValue.TotalSeconds
                : 60;

            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter).ToString();

            await context.HttpContext.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = 429,
                    title = "Too Many Requests",
                    detail = "You have exceeded the allowed number of requests. Please try again later.",
                    type = "https://tools.ietf.org/html/rfc6585#section-4"
                }, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                }), cancellationToken);
        };

        // Strict policy for authentication endpoints (login, register, forgot-password)
        // 5 requests per minute per IP
        options.AddFixedWindowLimiter("auth", limiterOptions =>
        {
            limiterOptions.PermitLimit = 5;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0;
        });

        // Moderate policy for token refresh and password reset
        // 10 requests per minute per IP
        options.AddFixedWindowLimiter("token", limiterOptions =>
        {
            limiterOptions.PermitLimit = 10;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0;
        });

        // General policy for all other endpoints
        // 100 requests per minute per IP
        options.AddFixedWindowLimiter("general", limiterOptions =>
        {
            limiterOptions.PermitLimit = 100;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0;
        });

        // Global fallback policy
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 200,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));
    });
}

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        if (allowedOrigins is { Length: > 0 })
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Apply database migrations and seed data (production/staging only, not in tests)
var autoMigrate = app.Configuration.GetValue<bool>("Database:AutoMigrate", false);
var autoSeed = app.Configuration.GetValue<bool>("Database:AutoSeed", false);

if ((autoMigrate || autoSeed) && !app.Environment.IsDevelopment())
{
    await DatabaseSeeder.SeedAsync(app.Services);
}

// Configure the HTTP request pipeline.

// Correlation ID middleware (first in pipeline for tracing)
app.UseCorrelationId();

// Request logging middleware
app.UseRequestLogging();

// Exception handling middleware (should be early in the pipeline)
app.UseExceptionHandling();

// Security headers
app.UseSecurityHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("Default");

// Rate limiting middleware (only when enabled)
if (enableRateLimiting && !app.Environment.IsDevelopment())
{
    app.UseRateLimiter();
}

app.UseAuthentication();

app.UseAuthorization();

// Health check endpoints
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Liveness: no checks, just confirms the app is running
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready") // Readiness: database check
});

app.MapControllers();

app.Run();

// Required for integration testing with WebApplicationFactory
public partial class Program { }
