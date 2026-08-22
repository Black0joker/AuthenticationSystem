using IdentityAuth.Application.Common.Interfaces;
using IdentityAuth.Domain.Entities;

namespace IdentityAuth.Application.Security.Services;

public class SecurityEventService : ISecurityEventService
{
    private readonly ISecurityEventRepository _securityEventRepository;
    private readonly IApplicationDbContext _dbContext;

    public SecurityEventService(
        ISecurityEventRepository securityEventRepository,
        IApplicationDbContext dbContext)
    {
        _securityEventRepository = securityEventRepository;
        _dbContext = dbContext;
    }

    public async Task LogEventAsync(
        SecurityEventType eventType,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var securityEvent = new SecurityEvent
        {
            EventType = eventType,
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow
        };

        await _securityEventRepository.AddAsync(securityEvent, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
