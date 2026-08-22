using System.Net;
using System.Net.Http.Json;
using IdentityAuth.Application.Authentication.DTOs;
using IdentityAuth.Application.Authentication.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityAuth.IntegrationTests;

public class RefreshTokenTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RefreshTokenTests(CustomWebApplicationFactory factory)
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
    public async Task Login_ReturnsRefreshToken()
    {
        // Arrange & Act
        var (_, _, refreshToken) = await RegisterVerifyAndLoginAsync("refresh1@example.com");

        // Assert
        Assert.False(string.IsNullOrEmpty(refreshToken));
    }

    [Fact]
    public async Task Refresh_ValidToken_Returns200()
    {
        // Arrange
        var (_, _, refreshToken) = await RegisterVerifyAndLoginAsync("refresh2@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = refreshToken });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewAccessToken()
    {
        // Arrange
        var (_, _, refreshToken) = await RegisterVerifyAndLoginAsync("refresh3@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = refreshToken });
        var content = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();

        // Assert
        Assert.NotNull(content);
        Assert.False(string.IsNullOrEmpty(content.AccessToken));
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewRefreshToken()
    {
        // Arrange
        var (_, _, refreshToken) = await RegisterVerifyAndLoginAsync("refresh4@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = refreshToken });
        var content = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();

        // Assert
        Assert.NotNull(content);
        Assert.False(string.IsNullOrEmpty(content.RefreshToken));
    }

    [Fact]
    public async Task Refresh_RotatesToken_NewTokenDifferentFromOld()
    {
        // Arrange
        var (_, _, refreshToken) = await RegisterVerifyAndLoginAsync("refresh5@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = refreshToken });
        var content = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();

        // Assert - new refresh token is different from old one
        Assert.NotNull(content);
        Assert.NotEqual(refreshToken, content.RefreshToken);
    }

    [Fact]
    public async Task Refresh_UsedToken_Returns401()
    {
        // Arrange
        var (_, _, refreshToken) = await RegisterVerifyAndLoginAsync("refresh6@example.com");

        // Use the token once
        await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = refreshToken });

        // Act - try to reuse the same token
        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = refreshToken });

        // Assert - reuse detection should reject
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_UsedToken_ReturnsReuseMessage()
    {
        // Arrange
        var (_, _, refreshToken) = await RegisterVerifyAndLoginAsync("refresh7@example.com");

        // Use the token once
        await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = refreshToken });

        // Act - try to reuse the same token
        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = refreshToken });
        var content = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        Assert.NotNull(content);
        Assert.Contains("reuse detected", content.Detail);
    }

    [Fact]
    public async Task Refresh_InvalidToken_Returns401()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = "completely-invalid-token" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_EmptyToken_Returns400()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = "" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_ChainedRotation_Works()
    {
        // Arrange
        var (_, _, refreshToken1) = await RegisterVerifyAndLoginAsync("refresh8@example.com");

        // First refresh
        var response1 = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = refreshToken1 });
        var content1 = await response1.Content.ReadFromJsonAsync<RefreshTokenResponse>();

        // Act - second refresh with the new token
        var response2 = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = content1!.RefreshToken });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var content2 = await response2.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        Assert.NotNull(content2);
        Assert.False(string.IsNullOrEmpty(content2.AccessToken));
        Assert.False(string.IsNullOrEmpty(content2.RefreshToken));
    }

    [Fact]
    public async Task Refresh_ReuseDetection_RevokesAllUserTokens()
    {
        // Arrange - login twice to get two token families
        var email = "refresh9@example.com";
        var (_, _, refreshToken1) = await RegisterVerifyAndLoginAsync(email);

        // First refresh (invalidates refreshToken1)
        var response1 = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = refreshToken1 });
        var content1 = await response1.Content.ReadFromJsonAsync<RefreshTokenResponse>();

        // Simulate token theft: reuse the old token
        var reuseResponse = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = refreshToken1 });

        // Assert - reuse detected
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

        // The new token from the first refresh should also be revoked
        var response3 = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = content1!.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, response3.StatusCode);
    }

    private record ProblemDetails
    {
        public int? Status { get; set; }
        public string? Title { get; set; }
        public string? Detail { get; set; }
    }
}
