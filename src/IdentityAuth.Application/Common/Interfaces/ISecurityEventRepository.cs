using IdentityAuth.Domain.Entities;

namespace IdentityAuth.Application.Common.Interfaces;

public interface ISecurityEventRepository
{
    Task AddAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityEvent>> GetByUserIdAsync(Guid userId, int pageSize = 50, int page = 1, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityEvent>> GetByEventTypeAsync(SecurityEventType eventType, int pageSize = 50, int page = 1, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityEvent>> GetRecentAsync(int pageSize = 50, CancellationToken cancellationToken = default);
}
