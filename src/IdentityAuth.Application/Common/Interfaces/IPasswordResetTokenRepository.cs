using IdentityAuth.Domain.Entities;

namespace IdentityAuth.Application.Common.Interfaces;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
    Task InvalidateAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);
}
