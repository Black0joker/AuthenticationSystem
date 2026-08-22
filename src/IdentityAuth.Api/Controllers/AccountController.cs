using System.Security.Claims;
using IdentityAuth.Application.Authentication.DTOs;
using IdentityAuth.Application.Authentication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAuth.Api.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAccountService accountService,
        ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    /// <summary>
    /// Changes the authenticated user's password.
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ChangePasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        if (userId == Guid.Empty)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "Unable to determine user identity."
            });
        }

        _logger.LogInformation("Password change requested for user: {UserId}", userId);

        var response = await _accountService.ChangePasswordAsync(userId, request, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Password Change Failed",
                Detail = response.Message
            });
        }

        _logger.LogInformation("Password changed successfully for user: {UserId}", userId);

        return Ok(response);
    }

    /// <summary>
    /// Updates the authenticated user's profile (first name, last name).
    /// </summary>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(UpdateProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        if (userId == Guid.Empty)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "Unable to determine user identity."
            });
        }

        _logger.LogInformation("Profile update requested for user: {UserId}", userId);

        var response = await _accountService.UpdateProfileAsync(userId, request, cancellationToken);

        _logger.LogInformation("Profile updated successfully for user: {UserId}", userId);

        return Ok(response);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;
    }
}
