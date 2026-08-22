using System.Net;
using System.Net.Http.Json;
using IdentityAuth.Application.Authentication.DTOs;
using IdentityAuth.Application.Authentication.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityAuth.IntegrationTests;

public class PasswordResetTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PasswordResetTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Guid> RegisterAndVerifyUserAsync(string email)
    {
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

        return registerResult.Id;
    }

    [Fact]
    public async Task ForgotPassword_ExistingEmail_Returns200()
    {
        // Arrange
        var email = "reset1@example.com";
        await RegisterAndVerifyUserAsync(email);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordRequest { Email = email });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_ExistingEmail_ReturnsGenericMessage()
    {
        // Arrange
        var email = "reset2@example.com";
        await RegisterAndVerifyUserAsync(email);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordRequest { Email = email });
        var content = await response.Content.ReadFromJsonAsync<ForgotPasswordResponse>();

        // Assert
        Assert.NotNull(content);
        Assert.Contains("If an account with that email exists", content.Message);
    }

    [Fact]
    public async Task ForgotPassword_NonExistingEmail_Returns200()
    {
        // Act - request reset for non-existent email
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordRequest { Email = "nonexistent@example.com" });

        // Assert - same response as existing email (prevents enumeration)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<ForgotPasswordResponse>();
        Assert.NotNull(content);
        Assert.Contains("If an account with that email exists", content.Message);
    }

    [Fact]
    public async Task ForgotPassword_EmptyEmail_Returns400()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordRequest { Email = "" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ValidToken_Returns200()
    {
        // Arrange
        var email = "reset3@example.com";
        var userId = await RegisterAndVerifyUserAsync(email);

        // Generate reset token
        using var scope = _factory.Services.CreateScope();
        var resetService = scope.ServiceProvider.GetRequiredService<IPasswordResetService>();
        var resetToken = await resetService.GenerateResetTokenAsync(userId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest
            {
                Token = resetToken,
                NewPassword = "NewStrongPass2!",
                ConfirmPassword = "NewStrongPass2!"
            });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<ResetPasswordResponse>();
        Assert.NotNull(content);
        Assert.True(content.Success);
    }

    [Fact]
    public async Task ResetPassword_ValidToken_CanLoginWithNewPassword()
    {
        // Arrange
        var email = "reset4@example.com";
        var userId = await RegisterAndVerifyUserAsync(email);

        // Generate reset token and reset password
        using var scope = _factory.Services.CreateScope();
        var resetService = scope.ServiceProvider.GetRequiredService<IPasswordResetService>();
        var resetToken = await resetService.GenerateResetTokenAsync(userId);

        await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest
            {
                Token = resetToken,
                NewPassword = "NewStrongPass2!",
                ConfirmPassword = "NewStrongPass2!"
            });

        // Act - login with new password
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = "NewStrongPass2!" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ValidToken_OldPasswordNoLongerWorks()
    {
        // Arrange
        var email = "reset5@example.com";
        var userId = await RegisterAndVerifyUserAsync(email);

        // Generate reset token and reset password
        using var scope = _factory.Services.CreateScope();
        var resetService = scope.ServiceProvider.GetRequiredService<IPasswordResetService>();
        var resetToken = await resetService.GenerateResetTokenAsync(userId);

        await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest
            {
                Token = resetToken,
                NewPassword = "NewStrongPass2!",
                ConfirmPassword = "NewStrongPass2!"
            });

        // Act - try login with old password
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = "StrongPass1!" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_UsedToken_Returns400()
    {
        // Arrange
        var email = "reset6@example.com";
        var userId = await RegisterAndVerifyUserAsync(email);

        using var scope = _factory.Services.CreateScope();
        var resetService = scope.ServiceProvider.GetRequiredService<IPasswordResetService>();
        var resetToken = await resetService.GenerateResetTokenAsync(userId);

        // Use the token once
        await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest
            {
                Token = resetToken,
                NewPassword = "NewStrongPass2!",
                ConfirmPassword = "NewStrongPass2!"
            });

        // Act - try to use the same token again
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest
            {
                Token = resetToken,
                NewPassword = "AnotherPass3!",
                ConfirmPassword = "AnotherPass3!"
            });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_Returns400()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest
            {
                Token = "completely-invalid-token",
                NewPassword = "NewStrongPass2!",
                ConfirmPassword = "NewStrongPass2!"
            });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_MismatchedPasswords_Returns400()
    {
        // Arrange
        var email = "reset7@example.com";
        var userId = await RegisterAndVerifyUserAsync(email);

        using var scope = _factory.Services.CreateScope();
        var resetService = scope.ServiceProvider.GetRequiredService<IPasswordResetService>();
        var resetToken = await resetService.GenerateResetTokenAsync(userId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest
            {
                Token = resetToken,
                NewPassword = "NewStrongPass2!",
                ConfirmPassword = "DifferentPass3!"
            });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WeakPassword_Returns400()
    {
        // Arrange
        var email = "reset8@example.com";
        var userId = await RegisterAndVerifyUserAsync(email);

        using var scope = _factory.Services.CreateScope();
        var resetService = scope.ServiceProvider.GetRequiredService<IPasswordResetService>();
        var resetToken = await resetService.GenerateResetTokenAsync(userId);

        // Act - password without required complexity
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest
            {
                Token = resetToken,
                NewPassword = "weak",
                ConfirmPassword = "weak"
            });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_EmptyToken_Returns400()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest
            {
                Token = "",
                NewPassword = "NewStrongPass2!",
                ConfirmPassword = "NewStrongPass2!"
            });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_CaseInsensitiveEmail_Works()
    {
        // Arrange
        var email = "resetcase@example.com";
        await RegisterAndVerifyUserAsync(email);

        // Act - request with different case
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordRequest { Email = "RESETCASE@EXAMPLE.COM" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
