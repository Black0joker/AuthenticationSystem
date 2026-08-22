using IdentityAuth.Application.Common.Interfaces;
using IdentityAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityAuth.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _context;

    public RefreshTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens.AddAsync(token, cancellationToken);
    }

    public async Task RevokeAsync(RefreshToken token, string? revokedByIp, CancellationToken cancellationToken = default)
    {
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = revokedByIp;
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, string? revokedByIp, CancellationToken cancellationToken = default)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = revokedByIp;
        }
    }
}
