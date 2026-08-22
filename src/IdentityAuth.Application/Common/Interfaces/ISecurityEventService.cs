using IdentityAuth.Domain.Entities;

namespace IdentityAuth.Application.Common.Interfaces;

public interface ISecurityEventService
{
    /// <summary>
    /// Logs a security event asynchronously.
    /// </summary>
    Task LogEventAsync(
        SecurityEventType eventType,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? metadata = null,
        CancellationToken cancellationToken = default);
}
