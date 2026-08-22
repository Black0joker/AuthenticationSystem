using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAuth.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(ILogger<ProfileController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns the authenticated user's profile information from the JWT.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value
                 ?? User.FindFirst("email")?.Value;
        var firstName = User.FindFirst(ClaimTypes.GivenName)?.Value
                     ?? User.FindFirst("given_name")?.Value;
        var lastName = User.FindFirst(ClaimTypes.Surname)?.Value
                    ?? User.FindFirst("family_name")?.Value;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        _logger.LogInformation("Profile requested for user: {UserId}", userId);

        return Ok(new ProfileResponse
        {
            UserId = Guid.TryParse(userId, out var id) ? id : Guid.Empty,
            Email = email ?? string.Empty,
            FirstName = firstName ?? string.Empty,
            LastName = lastName ?? string.Empty,
            Roles = roles
        });
    }

    /// <summary>
    /// Admin-only endpoint for testing role-based authorization.
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(AnonymousResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetAdminData()
    {
        return Ok(new AnonymousResult { Message = "Admin access granted." });
    }
}

public class ProfileResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class AnonymousResult
{
    public string Message { get; set; } = string.Empty;
}
