using IdentityAuth.Application.Authentication.DTOs;
using IdentityAuth.Application.Authentication.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace IdentityAuth.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IRegistrationService _registrationService;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly ILoginService _loginService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IRegistrationService registrationService,
        IEmailVerificationService emailVerificationService,
        ILoginService loginService,
        IRefreshTokenService refreshTokenService,
        IPasswordResetService passwordResetService,
        ILogger<AuthController> logger)
    {
        _registrationService = registrationService;
        _emailVerificationService = emailVerificationService;
        _loginService = loginService;
        _refreshTokenService = refreshTokenService;
        _passwordResetService = passwordResetService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

        var response = await _registrationService.RegisterAsync(request, cancellationToken);

        _logger.LogInformation("User registered successfully: {UserId}", response.Id);

        return CreatedAtAction(
            actionName: null,
            routeValues: null,
            value: response);
    }

    /// <summary>
    /// Verifies a user's email address using a verification token.
    /// </summary>
    [HttpPost("verify-email")]
    [EnableRateLimiting("token")]
    [ProducesResponseType(typeof(VerifyEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email verification attempt");

        var response = await _emailVerificationService.VerifyEmailAsync(request, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Verification Failed",
                Detail = response.Message
            });
        }

        _logger.LogInformation("Email verified successfully");

        return Ok(response);
    }

    /// <summary>
    /// Authenticates a user and returns user information with access and refresh tokens.
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var response = await _loginService.LoginAsync(request, ipAddress, cancellationToken);

        _logger.LogInformation("User logged in successfully: {UserId}", response.UserId);

        return Ok(response);
    }

    /// <summary>
    /// Refreshes the access token using a valid refresh token. Rotates the refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [EnableRateLimiting("token")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Token refresh attempt");

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var response = await _refreshTokenService.RefreshTokenAsync(request, ipAddress, cancellationToken);

        _logger.LogInformation("Token refreshed successfully");

        return Ok(response);
    }

    /// <summary>
    /// Logs out the user by revoking the refresh token.
    /// </summary>
    [HttpPost("logout")]
    [EnableRateLimiting("token")]
    [ProducesResponseType(typeof(LogoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Logout attempt");

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _refreshTokenService.RevokeTokenAsync(request.RefreshToken, ipAddress, cancellationToken);

        return Ok(new LogoutResponse
        {
            Message = "Logged out successfully."
        });
    }

    /// <summary>
    /// Initiates the password reset flow. Sends a reset token to the user's email.
    /// Returns a generic response regardless of whether the email exists (prevents enumeration).
    /// </summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset requested");

        var response = await _passwordResetService.ForgotPasswordAsync(request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Resets the user's password using a valid reset token.
    /// </summary>
    [HttpPost("reset-password")]
    [EnableRateLimiting("token")]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset attempt");

        var response = await _passwordResetService.ResetPasswordAsync(request, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Reset Failed",
                Detail = response.Message
            });
        }

        _logger.LogInformation("Password reset successfully");

        return Ok(response);
    }
}
