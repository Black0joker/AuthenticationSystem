using IdentityAuth.Application.Authentication.DTOs;

namespace IdentityAuth.Application.Authentication.Services;

public interface IRefreshTokenService
{
    /// <summary>
    /// Generates a refresh token for a user and returns the plain-text token.
    /// </summary>
    Task<string> GenerateRefreshTokenAsync(Guid userId, string? ipAddress = null, Guid? familyId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the access token using a valid refresh token. Rotates the refresh token.
    /// </summary>
    Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress = null, CancellationToken cancellationToken = default);
}
