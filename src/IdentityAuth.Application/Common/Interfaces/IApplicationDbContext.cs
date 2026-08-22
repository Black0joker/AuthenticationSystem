using Microsoft.EntityFrameworkCore;

namespace IdentityAuth.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Domain.Entities.User> Users { get; }
    DbSet<Domain.Entities.Role> Roles { get; }
    DbSet<Domain.Entities.Permission> Permissions { get; }
    DbSet<Domain.Entities.RefreshToken> RefreshTokens { get; }
    DbSet<Domain.Entities.EmailVerificationToken> EmailVerificationTokens { get; }
    DbSet<Domain.Entities.PasswordResetToken> PasswordResetTokens { get; }
    DbSet<Domain.Entities.SecurityEvent> SecurityEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
