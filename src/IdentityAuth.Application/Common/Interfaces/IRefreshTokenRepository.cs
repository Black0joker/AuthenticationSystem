using IdentityAuth.Domain.Entities;

namespace IdentityAuth.Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task RevokeAsync(RefreshToken token, string? revokedByIp, CancellationToken cancellationToken = default);
    Task RevokeAllUserTokensAsync(Guid userId, string? revokedByIp, CancellationToken cancellationToken = default);
}
