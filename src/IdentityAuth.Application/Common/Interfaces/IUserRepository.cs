using IdentityAuth.Domain.Entities;

namespace IdentityAuth.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailNormalizedAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailNormalizedAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<string?> GetSecurityStampAsync(Guid userId, CancellationToken cancellationToken = default);
}
