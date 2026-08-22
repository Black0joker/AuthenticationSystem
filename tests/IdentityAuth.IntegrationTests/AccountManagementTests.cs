using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdentityAuth.Application.Authentication.DTOs;
using IdentityAuth.Application.Authentication.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityAuth.IntegrationTests;

public class AccountManagementTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AccountManagementTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(Guid UserId, string AccessToken)> RegisterVerifyAndLoginAsync(string email, string password = "StrongPass1!")
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

        // Verify email
        using var scope = _factory.Services.CreateScope();
        var verificationService = scope.ServiceProvider.GetRequiredService<IEmailVerificationService>();
        var verifyToken = await verificationService.GenerateVerificationTokenAsync(registerResult!.Id);
        await _client.PostAsJsonAsync("/api/auth/verify-email",
            new VerifyEmailRequest { Token = verifyToken });

        // Login
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = password });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        return (registerResult.Id, loginResult!.AccessToken);
    }

    // === Change Password Tests ===

    [Fact]
    public async Task ChangePassword_ValidRequest_Returns200()
    {
        // Arrange
        var (_, accessToken) = await RegisterVerifyAndLoginAsync("acct1@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/account/change-password",
            new ChangePasswordRequest
            {
                CurrentPassword = "StrongPass1!",
                NewPassword = "NewStrongPass2!",
                ConfirmPassword = "NewStrongPass2!"
            });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<ChangePasswordResponse>();
        Assert.NotNull(content);
        Assert.True(content.Success);
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_CanLoginWithNewPassword()
    {
        // Arrange
        var email = "acct2@example.com";
        var (_, accessToken) = await RegisterVerifyAndLoginAsync(email);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        // Change password
        await _client.PostAsJsonAsync("/api/account/change-password",
            new ChangePasswordRequest
            {
                CurrentPassword = "StrongPass1!",
                NewPassword = "NewStrongPass2!",
                ConfirmPassword = "NewStrongPass2!"
            });

        // Act - login with new password
        _client.DefaultRequestHeaders.Authorization = null;
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = "NewStrongPass2!" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_OldPasswordNoLongerWorks()
    {
        // Arrange
        var email = "acct3@example.com";
        var (_, accessToken) = await RegisterVerifyAndLoginAsync(email);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        // Change password
        await _client.PostAsJsonAsync("/api/account/change-password",
            new ChangePasswordRequest
            {
                CurrentPassword = "StrongPass1!",
                NewPassword = "NewStrongPass2!",
                ConfirmPassword = "NewStrongPass2!"
            });

        // Act - try login with old password
        _client.DefaultRequestHeaders.Authorization = null;
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = "StrongPass1!" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns400()
    {
        // Arrange
        var (_, accessToken) = await RegisterVerifyAndLoginAsync("acct4@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/account/change-password",
            new ChangePasswordRequest
            {
                CurrentPassword = "WrongPassword1!",
                NewPassword = "NewStrongPass2!",
                ConfirmPassword = "NewStrongPass2!"
            });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_SameAsCurrent_Returns400()
    {
        // Arrange
        var (_, accessToken) = await RegisterVerifyAndLoginAsync("acct5@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/account/change-password",
            new ChangePasswordRequest
            {
                CurrentPassword = "StrongPass1!",
                NewPassword = "StrongPass1!",
                ConfirmPassword = "StrongPass1!"
            });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_MismatchedConfirmation_Returns400()
    {
        // Arrange
        var (_, accessToken) = await RegisterVerifyAndLoginAsync("acct6@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/account/change-password",
            new ChangePasswordRequest
            {
                CurrentPassword = "StrongPass1!",
                NewPassword = "NewStrongPass2!",
                ConfirmPassword = "DifferentPass3!"
            });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_NoAuth_Returns401()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/account/change-password",
            new ChangePasswordRequest
            {
                CurrentPassword = "StrongPass1!",
                NewPassword = "NewStrongPass2!",
                ConfirmPassword = "NewStrongPass2!"
            });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // === Update Profile Tests ===

    [Fact]
    public async Task UpdateProfile_ValidRequest_Returns200()
    {
        // Arrange
        var (_, accessToken) = await RegisterVerifyAndLoginAsync("acct7@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await _client.PutAsJsonAsync("/api/account/profile",
            new UpdateProfileRequest
            {
                FirstName = "Updated",
                LastName = "Name"
            });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ValidRequest_ReturnsUpdatedData()
    {
        // Arrange
        var (_, accessToken) = await RegisterVerifyAndLoginAsync("acct8@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await _client.PutAsJsonAsync("/api/account/profile",
            new UpdateProfileRequest
            {
                FirstName = "Updated",
                LastName = "Name"
            });
        var content = await response.Content.ReadFromJsonAsync<UpdateProfileResponse>();

        // Assert
        Assert.NotNull(content);
        Assert.Equal("Updated", content.FirstName);
        Assert.Equal("Name", content.LastName);
        Assert.Equal("acct8@example.com", content.Email);
    }

    [Fact]
    public async Task UpdateProfile_NoAuth_Returns401()
    {
        // Act
        var response = await _client.PutAsJsonAsync("/api/account/profile",
            new UpdateProfileRequest
            {
                FirstName = "Updated",
                LastName = "Name"
            });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_EmptyFirstName_Returns400()
    {
        // Arrange
        var (_, accessToken) = await RegisterVerifyAndLoginAsync("acct9@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await _client.PutAsJsonAsync("/api/account/profile",
            new UpdateProfileRequest
            {
                FirstName = "",
                LastName = "Name"
            });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_EmptyLastName_Returns400()
    {
        // Arrange
        var (_, accessToken) = await RegisterVerifyAndLoginAsync("acct10@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await _client.PutAsJsonAsync("/api/account/profile",
            new UpdateProfileRequest
            {
                FirstName = "Test",
                LastName = ""
            });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
