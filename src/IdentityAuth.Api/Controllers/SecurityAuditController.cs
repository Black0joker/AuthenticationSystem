using IdentityAuth.Application.Common.Interfaces;
using IdentityAuth.Application.Security.DTOs;
using IdentityAuth.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAuth.Api.Controllers;

[ApiController]
[Route("api/security")]
[Authorize(Policy = "AdminOnly")]
public class SecurityAuditController : ControllerBase
{
    private readonly ISecurityEventRepository _securityEventRepository;
    private readonly ILogger<SecurityAuditController> _logger;

    public SecurityAuditController(
        ISecurityEventRepository securityEventRepository,
        ILogger<SecurityAuditController> logger)
    {
        _securityEventRepository = securityEventRepository;
        _logger = logger;
    }

    /// <summary>
    /// Returns recent security events (admin only).
    /// </summary>
    [HttpGet("events")]
    [ProducesResponseType(typeof(SecurityEventListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRecentEvents(
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Security audit log requested");

        var events = await _securityEventRepository.GetRecentAsync(pageSize, cancellationToken);

        var response = new SecurityEventListResponse
        {
            Events = events.Select(MapToResponse).ToList(),
            TotalCount = events.Count,
            Page = 1,
            PageSize = pageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Returns security events for a specific user (admin only).
    /// </summary>
    [HttpGet("events/user/{userId}")]
    [ProducesResponseType(typeof(SecurityEventListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserEvents(
        Guid userId,
        [FromQuery] int pageSize = 50,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Security events requested for user: {UserId}", userId);

        var events = await _securityEventRepository.GetByUserIdAsync(userId, pageSize, page, cancellationToken);

        var response = new SecurityEventListResponse
        {
            Events = events.Select(MapToResponse).ToList(),
            TotalCount = events.Count,
            Page = page,
            PageSize = pageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Returns security events filtered by event type (admin only).
    /// </summary>
    [HttpGet("events/type/{eventType}")]
    [ProducesResponseType(typeof(SecurityEventListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEventsByType(
        string eventType,
        [FromQuery] int pageSize = 50,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<SecurityEventType>(eventType, ignoreCase: true, out var parsedType))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Event Type",
                Detail = $"'{eventType}' is not a valid security event type."
            });
        }

        _logger.LogInformation("Security events requested for type: {EventType}", parsedType);

        var events = await _securityEventRepository.GetByEventTypeAsync(parsedType, pageSize, page, cancellationToken);

        var response = new SecurityEventListResponse
        {
            Events = events.Select(MapToResponse).ToList(),
            TotalCount = events.Count,
            Page = page,
            PageSize = pageSize
        };

        return Ok(response);
    }

    private static SecurityEventResponse MapToResponse(SecurityEvent entity)
    {
        return new SecurityEventResponse
        {
            Id = entity.Id,
            UserId = entity.UserId,
            EventType = entity.EventType.ToString(),
            IpAddress = entity.IpAddress,
            UserAgent = entity.UserAgent,
            Metadata = entity.Metadata,
            CreatedAt = entity.CreatedAt
        };
    }
}
