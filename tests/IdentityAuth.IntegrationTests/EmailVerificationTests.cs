using System.Net;
using System.Net.Http.Json;
using IdentityAuth.Application.Authentication.DTOs;
using IdentityAuth.Application.Authentication.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityAuth.IntegrationTests;

public class EmailVerificationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EmailVerificationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(Guid UserId, string Token)> RegisterAndGenerateTokenAsync(string email)
    {
        // Register a user
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = "StrongPass1!",
            FirstName = "Test",
            LastName = "User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();

        // Generate verification token using the service
        using var scope = _factory.Services.CreateScope();
        var verificationService = scope.ServiceProvider.GetRequiredService<IEmailVerificationService>();
        var token = await verificationService.GenerateVerificationTokenAsync(registerResult!.Id);

        return (registerResult.Id, token);
    }

    [Fact]
    public async Task VerifyEmail_ValidToken_Returns200()
    {
        // Arrange
        var (_, token) = await RegisterAndGenerateTokenAsync("verify1@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email",
            new VerifyEmailRequest { Token = token });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_ValidToken_ReturnsSuccessMessage()
    {
        // Arrange
        var (_, token) = await RegisterAndGenerateTokenAsync("verify2@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email",
            new VerifyEmailRequest { Token = token });
        var content = await response.Content.ReadFromJsonAsync<VerifyEmailResponse>();

        // Assert
        Assert.NotNull(content);
        Assert.True(content.Success);
        Assert.Equal("Email verified successfully.", content.Message);
    }

    [Fact]
    public async Task VerifyEmail_UsedToken_Returns400()
    {
        // Arrange
        var (_, token) = await RegisterAndGenerateTokenAsync("verify3@example.com");

        // Use the token once
        await _client.PostAsJsonAsync("/api/auth/verify-email",
            new VerifyEmailRequest { Token = token });

        // Act - try to use the same token again
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email",
            new VerifyEmailRequest { Token = token });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_UsedToken_ReturnsAlreadyUsedMessage()
    {
        // Arrange
        var (_, token) = await RegisterAndGenerateTokenAsync("verify4@example.com");

        // Use the token once
        await _client.PostAsJsonAsync("/api/auth/verify-email",
            new VerifyEmailRequest { Token = token });

        // Act - try to use the same token again
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email",
            new VerifyEmailRequest { Token = token });
        var content = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        Assert.NotNull(content);
        Assert.Contains("already been used", content.Detail);
    }

    [Fact]
    public async Task VerifyEmail_InvalidToken_Returns400()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email",
            new VerifyEmailRequest { Token = "invalid-token-that-does-not-exist" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_InvalidToken_ReturnsInvalidMessage()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email",
            new VerifyEmailRequest { Token = "invalid-token-that-does-not-exist" });
        var content = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        Assert.NotNull(content);
        Assert.Contains("Invalid verification token", content.Detail);
    }

    [Fact]
    public async Task VerifyEmail_EmptyToken_Returns400()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email",
            new VerifyEmailRequest { Token = "" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_NewTokenInvalidatesOldToken()
    {
        // Arrange
        var email = "verify5@example.com";
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = "StrongPass1!",
            FirstName = "Test",
            LastName = "User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();

        // Generate first token
        using var scope = _factory.Services.CreateScope();
        var verificationService = scope.ServiceProvider.GetRequiredService<IEmailVerificationService>();
        var firstToken = await verificationService.GenerateVerificationTokenAsync(registerResult!.Id);

        // Generate second token (should invalidate first)
        var secondToken = await verificationService.GenerateVerificationTokenAsync(registerResult.Id);

        // Act - try to use the first (now invalidated) token
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email",
            new VerifyEmailRequest { Token = firstToken });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_SecondTokenWorks_AfterFirstInvalidated()
    {
        // Arrange
        var email = "verify6@example.com";
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = "StrongPass1!",
            FirstName = "Test",
            LastName = "User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();

        // Generate first token
        using var scope = _factory.Services.CreateScope();
        var verificationService = scope.ServiceProvider.GetRequiredService<IEmailVerificationService>();
        await verificationService.GenerateVerificationTokenAsync(registerResult!.Id);

        // Generate second token
        var secondToken = await verificationService.GenerateVerificationTokenAsync(registerResult.Id);

        // Act - use the second token
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email",
            new VerifyEmailRequest { Token = secondToken });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private record ProblemDetails
    {
        public int? Status { get; set; }
        public string? Title { get; set; }
        public string? Detail { get; set; }
    }
}
