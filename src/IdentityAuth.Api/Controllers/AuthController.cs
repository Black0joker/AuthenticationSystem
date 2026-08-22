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
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IRegistrationService registrationService,
        IEmailVerificationService emailVerificationService,
        ILogger<AuthController> logger)
    {
        _registrationService = registrationService;
        _emailVerificationService = emailVerificationService;
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
}
