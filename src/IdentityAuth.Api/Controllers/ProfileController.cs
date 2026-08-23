using System.Security.Claims;
using IdentityAuth.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAuth.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IUserRepository userRepository, ILogger<ProfileController> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Returns the authenticated user's profile information verified against the database.
    /// Ensures the user still exists and is not locked before returning data.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new AnonymousResult { Message = "Invalid token." });
        }

        // Verify user still exists and is not locked by querying the database
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Profile request for deleted user: {UserId}", userId);
            return Unauthorized(new AnonymousResult { Message = "Invalid token." });
        }

        if (user.IsLocked)
        {
            _logger.LogWarning("Profile request for locked user: {UserId}", userId);
            return Unauthorized(new AnonymousResult { Message = "Invalid token." });
        }

        _logger.LogInformation("Profile requested for user: {UserId}", userId);

        return Ok(new ProfileResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
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
