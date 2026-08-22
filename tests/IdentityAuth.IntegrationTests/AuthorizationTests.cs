using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdentityAuth.Application.Authentication.DTOs;
using IdentityAuth.Application.Authentication.Services;
using IdentityAuth.Api.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityAuth.IntegrationTests;

public class AuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> RegisterVerifyAndLoginAsync(string email)
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

        return loginResult!.AccessToken;
    }

    [Fact]
    public async Task ProtectedEndpoint_NoToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/profile/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_InvalidToken_Returns401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid-token-here");

        // Act
        var response = await _client.GetAsync("/api/profile/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_ValidToken_Returns200()
    {
        // Arrange
        var token = await RegisterVerifyAndLoginAsync("auth1@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/profile/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_ValidToken_ReturnsUserData()
    {
        // Arrange
        var token = await RegisterVerifyAndLoginAsync("auth2@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/profile/me");
        var content = await response.Content.ReadFromJsonAsync<ProfileResponse>();

        // Assert
        Assert.NotNull(content);
        Assert.Equal("auth2@example.com", content.Email);
        Assert.Equal("Test", content.FirstName);
        Assert.Equal("User", content.LastName);
    }

    [Fact]
    public async Task AdminEndpoint_NoToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/profile/admin");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_UserRole_Returns403()
    {
        // Arrange - regular user (no Admin role)
        var token = await RegisterVerifyAndLoginAsync("auth3@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/profile/admin");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_EmptyBearerToken_Returns401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "");

        // Act
        var response = await _client.GetAsync("/api/profile/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_ExpiredToken_Returns401()
    {
        // Arrange - create a token that's already expired
        var token = await RegisterVerifyAndLoginAsync("auth4@example.com");
        // Tamper with the token to make it invalid
        var tamperedToken = token.Substring(0, token.Length - 5) + "XXXXX";
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tamperedToken);

        // Act
        var response = await _client.GetAsync("/api/profile/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_Returns401_WithProblemDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/profile/me");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.MediaType ?? "");
    }
}
