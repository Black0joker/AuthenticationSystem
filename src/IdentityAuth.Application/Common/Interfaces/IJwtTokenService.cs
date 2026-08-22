using IdentityAuth.Domain.Entities;

namespace IdentityAuth.Application.Common.Interfaces;

public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT access token for the given user.
    /// </summary>
    string GenerateAccessToken(User user);
}
