using IdentityAuth.Application.Common.Interfaces;
using IdentityAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityAuth.Infrastructure.Persistence.Repositories;

public class SecurityEventRepository : ISecurityEventRepository
{
    private readonly ApplicationDbContext _context;

    public SecurityEventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
    {
        await _context.SecurityEvents.AddAsync(securityEvent, cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityEvent>> GetByUserIdAsync(Guid userId, int pageSize = 50, int page = 1, CancellationToken cancellationToken = default)
    {
        return await _context.SecurityEvents
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityEvent>> GetByEventTypeAsync(SecurityEventType eventType, int pageSize = 50, int page = 1, CancellationToken cancellationToken = default)
    {
        return await _context.SecurityEvents
            .Where(e => e.EventType == eventType)
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityEvent>> GetRecentAsync(int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return await _context.SecurityEvents
            .OrderByDescending(e => e.CreatedAt)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
