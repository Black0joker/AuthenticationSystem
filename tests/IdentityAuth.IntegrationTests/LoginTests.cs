using System.Net;
using System.Net.Http.Json;
using IdentityAuth.Application.Authentication.DTOs;
using IdentityAuth.Application.Authentication.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityAuth.IntegrationTests;

public class LoginTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LoginTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Guid> RegisterAndVerifyUserAsync(string email, string password = "StrongPass1!")
    {
        // Register
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = password,
            FirstName = "Test",
            LastName = "User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();

        // Generate and use verification token
        using var scope = _factory.Services.CreateScope();
        var verificationService = scope.ServiceProvider.GetRequiredService<IEmailVerificationService>();
        var token = await verificationService.GenerateVerificationTokenAsync(registerResult!.Id);

        await _client.PostAsJsonAsync("/api/auth/verify-email",
            new VerifyEmailRequest { Token = token });

        return registerResult.Id;
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        // Arrange
        await RegisterAndVerifyUserAsync("login1@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "login1@example.com", Password = "StrongPass1!" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsUserData()
    {
        // Arrange
        var userId = await RegisterAndVerifyUserAsync("login2@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "login2@example.com", Password = "StrongPass1!" });
        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();

        // Assert
        Assert.NotNull(content);
        Assert.Equal(userId, content.UserId);
        Assert.Equal("login2@example.com", content.Email);
        Assert.Equal("Test", content.FirstName);
        Assert.Equal("User", content.LastName);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        // Arrange
        await RegisterAndVerifyUserAsync("login3@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "login3@example.com", Password = "WrongPass1!" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_NonExistentUser_Returns401()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "nonexistent@example.com", Password = "StrongPass1!" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnverifiedEmail_Returns403()
    {
        // Arrange - register but don't verify
        var registerRequest = new RegisterRequest
        {
            Email = "unverified@example.com",
            Password = "StrongPass1!",
            FirstName = "Test",
            LastName = "User"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "unverified@example.com", Password = "StrongPass1!" });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnverifiedEmail_ReturnsVerifyMessage()
    {
        // Arrange - register but don't verify
        var registerRequest = new RegisterRequest
        {
            Email = "unverified2@example.com",
            Password = "StrongPass1!",
            FirstName = "Test",
            LastName = "User"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "unverified2@example.com", Password = "StrongPass1!" });
        var content = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        Assert.NotNull(content);
        Assert.Contains("verify your email", content.Detail);
    }

    [Fact]
    public async Task Login_EmptyEmail_Returns400()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "", Password = "StrongPass1!" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_EmptyPassword_Returns400()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "test@example.com", Password = "" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_CaseInsensitiveEmail_Works()
    {
        // Arrange
        await RegisterAndVerifyUserAsync("logincasetest@example.com");

        // Act - login with different case
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "LOGINCASETEST@EXAMPLE.COM", Password = "StrongPass1!" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_MultipleFailedAttempts_LocksAccount()
    {
        // Arrange
        await RegisterAndVerifyUserAsync("locktest@example.com");

        // Act - 5 failed attempts
        for (int i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest { Email = "locktest@example.com", Password = "WrongPass1!" });
        }

        // Try with correct password after lockout
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "locktest@example.com", Password = "StrongPass1!" });

        // Assert - account should be locked
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_GenericErrorMessage_PreventsUserEnumeration()
    {
        // Act - try login with non-existent user
        var response1 = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "doesnotexist@example.com", Password = "StrongPass1!" });
        var content1 = await response1.Content.ReadFromJsonAsync<ProblemDetails>();

        // Register a user and try wrong password
        await RegisterAndVerifyUserAsync("exists@example.com");
        var response2 = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "exists@example.com", Password = "WrongPass1!" });
        var content2 = await response2.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert - same generic message for both cases
        Assert.NotNull(content1);
        Assert.NotNull(content2);
        Assert.Equal(content1.Detail, content2.Detail);
    }

    private record ProblemDetails
    {
        public int? Status { get; set; }
        public string? Title { get; set; }
        public string? Detail { get; set; }
    }
}
