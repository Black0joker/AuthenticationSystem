using IdentityAuth.Application.Authentication.DTOs;
using IdentityAuth.Application.Authentication.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAuth.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IRegistrationService _registrationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IRegistrationService registrationService,
        ILogger<AuthController> logger)
    {
        _registrationService = registrationService;
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
}
