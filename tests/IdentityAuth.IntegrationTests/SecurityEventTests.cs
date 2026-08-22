using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdentityAuth.Application.Authentication.DTOs;
using IdentityAuth.Application.Authentication.Services;
using IdentityAuth.Application.Common.Interfaces;
using IdentityAuth.Application.Security.DTOs;
using IdentityAuth.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityAuth.IntegrationTests;

public class SecurityEventTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SecurityEventTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(Guid UserId, string AccessToken, string RefreshToken)> RegisterVerifyAndLoginAsync(string email)
    {
        // Register
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = "StrongPass1!",
            FirstName = "Test",
            LastName = "User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();

        // Verify email
        using var scope = _factory.Services.CreateScope();
        var verificationService = scope.ServiceProvider.GetRequiredService<IEmailVerificationService>();
        var verifyToken = await verificationService.GenerateVerificationTokenAsync(registerResult!.Id);
        await _client.PostAsJsonAsync("/api/auth/verify-email",
            new VerifyEmailRequest { Token = verifyToken });

        // Login
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = "StrongPass1!" });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        return (registerResult.Id, loginResult!.AccessToken, loginResult.RefreshToken);
    }

    [Fact]
    public async Task Registration_LogsUserRegisteredEvent()
    {
        // Arrange
        var email = "secevent1@example.com";

        // Act
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "StrongPass1!",
            FirstName = "Test",
            LastName = "User"
        });

        // Assert - check security events in database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var events = dbContext.SecurityEvents
            .Where(e => e.EventType == SecurityEventType.UserRegistered)
            .ToList();

        Assert.NotEmpty(events);
    }

    [Fact]
    public async Task SuccessfulLogin_LogsLoginSucceededEvent()
    {
        // Arrange
        var email = "secevent2@example.com";
        var (userId, _, _) = await RegisterVerifyAndLoginAsync(email);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var events = dbContext.SecurityEvents
            .Where(e => e.UserId == userId && e.EventType == SecurityEventType.LoginSucceeded)
            .ToList();

        Assert.NotEmpty(events);
    }

    [Fact]
    public async Task FailedLogin_LogsLoginFailedEvent()
    {
        // Arrange
        var email = "secevent3@example.com";
        var (userId, _, _) = await RegisterVerifyAndLoginAsync(email);

        // Act - attempt login with wrong password
        await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = "WrongPassword1!" });

        // Assert
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var events = dbContext.SecurityEvents
            .Where(e => e.UserId == userId && e.EventType == SecurityEventType.LoginFailed)
            .ToList();

        Assert.NotEmpty(events);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        // Arrange
        var email = "secevent4@example.com";
        var (_, _, refreshToken) = await RegisterVerifyAndLoginAsync(email);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/logout",
            new LogoutRequest { RefreshToken = refreshToken });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<LogoutResponse>();
        Assert.NotNull(content);
        Assert.Equal("Logged out successfully.", content.Message);
    }

    [Fact]
    public async Task Logout_RevokedToken_CannotBeUsedForRefresh()
    {
        // Arrange
        var email = "secevent5@example.com";
        var (_, _, refreshToken) = await RegisterVerifyAndLoginAsync(email);

        // Logout
        await _client.PostAsJsonAsync("/api/auth/logout",
            new LogoutRequest { RefreshToken = refreshToken });

        // Act - try to use the revoked token
        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = refreshToken });

        // Assert - should fail because token was revoked
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_LogsLogoutEvent()
    {
        // Arrange
        var email = "secevent6@example.com";
        var (userId, _, refreshToken) = await RegisterVerifyAndLoginAsync(email);

        // Act
        await _client.PostAsJsonAsync("/api/auth/logout",
            new LogoutRequest { RefreshToken = refreshToken });

        // Assert
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var events = dbContext.SecurityEvents
            .Where(e => e.UserId == userId && e.EventType == SecurityEventType.Logout)
            .ToList();

        Assert.NotEmpty(events);
    }

    [Fact]
    public async Task Logout_InvalidToken_Returns200()
    {
        // Act - logout with invalid token should still return 200 (idempotent)
        var response = await _client.PostAsJsonAsync("/api/auth/logout",
            new LogoutRequest { RefreshToken = "invalid-token" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EmptyToken_Returns400()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/logout",
            new LogoutRequest { RefreshToken = "" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PasswordReset_LogsSecurityEvents()
    {
        // Arrange
        var email = "secevent7@example.com";
        var (userId, _, _) = await RegisterVerifyAndLoginAsync(email);

        // Act - request password reset
        await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordRequest { Email = email });

        // Assert
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var events = dbContext.SecurityEvents
            .Where(e => e.UserId == userId && e.EventType == SecurityEventType.PasswordResetRequested)
            .ToList();

        Assert.NotEmpty(events);
    }

    [Fact]
    public async Task TokenRefresh_LogsRevocationEvent()
    {
        // Arrange
        var email = "secevent8@example.com";
        var (userId, _, refreshToken) = await RegisterVerifyAndLoginAsync(email);

        // Act - refresh the token
        await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = refreshToken });

        // Assert
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var events = dbContext.SecurityEvents
            .Where(e => e.UserId == userId && e.EventType == SecurityEventType.RefreshTokenRevoked)
            .ToList();

        Assert.NotEmpty(events);
    }

    [Fact]
    public async Task SecurityAuditEndpoint_NoAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/security/events");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SecurityAuditEndpoint_UserRole_Returns403()
    {
        // Arrange - regular user
        var (_, accessToken, _) = await RegisterVerifyAndLoginAsync("secevent9@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await _client.GetAsync("/api/security/events");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
