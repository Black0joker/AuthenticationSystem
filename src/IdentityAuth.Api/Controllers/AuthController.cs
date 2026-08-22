using IdentityAuth.Application.Authentication.DTOs;
using IdentityAuth.Application.Authentication.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAuth.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IRegistrationService _registrationService;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly ILoginService _loginService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IRegistrationService registrationService,
        IEmailVerificationService emailVerificationService,
        ILoginService loginService,
        IRefreshTokenService refreshTokenService,
        ILogger<AuthController> logger)
    {
        _registrationService = registrationService;
        _emailVerificationService = emailVerificationService;
        _loginService = loginService;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
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
    [ProducesResponseType(typeof(VerifyEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
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
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
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
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
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
}
